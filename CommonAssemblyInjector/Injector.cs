using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonAssemblyInjector
{
    public class Injector
    {
        private const string FILE_TYPE_PROJECT = "*.csproj";
        private const string FILE_TYPE_ASSEMBLY = "*AssemblyInfo.cs";

        public string SolutionDir { get; set; }
        public string CommonAssemblyInfoPath { get; set; }
        public string TargetVersion { get; set; }
        public IEnumerable<string> IgnoreDirs { get; set; }

        private string CommonAssemblyInfoFileName => Path.GetFileName(CommonAssemblyInfoPath);

        public async Task TryAddCommonAssemblyToProjects()
        {
            IAsyncEnumerable<Tuple<string, string>> assemblyAndProjectFiles =
                GetRelevantProjectFilesAsync(SolutionDir, TargetVersion, CommonAssemblyInfoPath);

            await foreach (Tuple<string, string> assemblyAndProjectFile in assemblyAndProjectFiles)
            {
                await LinkCommonAssemblyInfoToProjectFilesAsync(assemblyAndProjectFile.Item2);
                await UpdateAssemblyInfoFileAsync(assemblyAndProjectFile.Item1, CommonAssemblyInfoPath);
            }
        }

        private async IAsyncEnumerable<Tuple<string, string>> GetRelevantProjectFilesAsync(string solutionDir, string version,
            string commonAssemblyPath)
        {
            if (!(await FileOperationHelper.GetFilesAsync(solutionDir, FILE_TYPE_ASSEMBLY, SearchOption.AllDirectories) is List<string>
                filesPaths))
            {
                yield break;
            }

            filesPaths.Remove(commonAssemblyPath);

            filesPaths.RemoveAll(s => IgnoreDirs.Any(s.StartsWith));

            IAsyncEnumerable<string> filteredFilePaths = FilterAssemblyFilesByVersionAsync(filesPaths, version);

            await foreach (string filteredFilePath in filteredFilePaths)
            {
                // This parent parent thing is a condition that needs to be met
                string path = Directory.GetParent(Directory.GetParent(filteredFilePath).FullName).FullName;


                yield return Tuple.Create(filteredFilePath, Directory.GetFiles(path, FILE_TYPE_PROJECT, SearchOption.TopDirectoryOnly)
                    .FirstOrDefault());
            }
        }

        private async IAsyncEnumerable<string> FilterAssemblyFilesByVersionAsync(IEnumerable<string> filePaths,
            string version)
        {
            foreach (string filePath in filePaths)
            {
                string pattern = $@"^\[assembly: AssemblyVersion\(\""{version}\""\)\]";
                string content = await File.ReadAllTextAsync(filePath);

                if (!new Regex(pattern, RegexOptions.Multiline).Match(content).Success)
                {
                    continue;
                }

                yield return filePath;
            }
        }

        private async Task LinkCommonAssemblyInfoToProjectFilesAsync(string projectFilePath)
        {
            string projectFileContent = await FileOperationHelper.ReadAllTextAsync(projectFilePath);

            if (string.IsNullOrWhiteSpace(projectFileContent))
            {
                return;
            }

            string relativeCommonAssemblyInFoPath = Path.GetRelativePath(Directory.GetParent(projectFilePath).FullName, CommonAssemblyInfoPath);

            if (projectFileContent.Contains(relativeCommonAssemblyInFoPath))
            {
                return; // Is already injected
            }

            Regex replaceRegEx = new Regex(@"<ItemGroup>", RegexOptions.RightToLeft);


            string newContent = replaceRegEx.Replace(projectFileContent, $@"<ItemGroup>
    <Compile Include=""{relativeCommonAssemblyInFoPath}"">
      <Link>Properties\{CommonAssemblyInfoFileName}</Link>
    </Compile>", 1);

            await FileOperationHelper.WriteAllTextAsync(projectFilePath, newContent);
        }

        private async Task UpdateAssemblyInfoFileAsync(string assemblyInfoPath, string commonAssemblyPath)
        {
            string[] commonAssemblyInfoContent = await File.ReadAllLinesAsync(commonAssemblyPath);

            string assemblyInfoContent =
                (from line in commonAssemblyInfoContent
                 let pattern = "[assembly: Assembly"
                 where line.StartsWith(pattern)
                 let regex = new Regex(@"(?<line>^\[assembly: Assembly\w+\()", RegexOptions.Multiline)
                 select regex.Match(line)
                    into match
                 where match.Success
                 select match.Captures.FirstOrDefault()?.Value)
                .Aggregate(await File.ReadAllTextAsync(assemblyInfoPath),
                    (current, s) =>
                        current.Replace($"\r\n{s ?? throw new InvalidOperationException()}", $"\r\n// {s}"));

            await FileOperationHelper.WriteAllTextAsync(assemblyInfoPath, assemblyInfoContent);
        }
    }
}