using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommonAssemblyInjector
{
    public static class Injector
    {
        private const string ONE_UP = "..\\";
        private const string FILE_TYPE_PROJECT = "*.csproj";
        private const string FILE_TYPE_ASSEMBLY = "*AssemblyInfo.cs";

        public static string SolutionDir { get; set; }
        public static string CommonAssemblyInfoPath { get; set; }
        public static string TargetVersion { get; set; }
        public static IEnumerable<string> IgnoreDirs { get; set; }

        public static async Task TryAddCommonAssemblyToProjects()
        {
            IAsyncEnumerable<string> filePaths =
                GetRelevantProjectFilesAsync(SolutionDir, TargetVersion, CommonAssemblyInfoPath);

            await foreach (string filePath in filePaths)
            {
                string content = await FileOperationHelper.ReadAllTextAsync(filePath);
                int depth = await FileOperationHelper.GetFileDepthDiffAsync(SolutionDir, filePath);

                await AddCommonAssemblyInfoToProjectFilesAsync(filePath, content, depth);
                await UpdateAssemblyInfoFileAsync(filePath, CommonAssemblyInfoPath);
            }
        }

        private static async IAsyncEnumerable<string> GetRelevantProjectFilesAsync(string solutionDir, string version,
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
                string path = Directory.GetParent(Directory.GetParent(filteredFilePath).FullName).FullName;
                yield return Directory.GetFiles(path, FILE_TYPE_PROJECT, SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
            }
        }

        private static async IAsyncEnumerable<string> FilterAssemblyFilesByVersionAsync(IEnumerable<string> filePaths,
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

        private static async Task AddCommonAssemblyInfoToProjectFilesAsync(string filePath, string content, int depth)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            Regex findRegEx = new Regex(@"<Compile Include=""(..\\)+CommonAssemblyInfo.cs"">",
               RegexOptions.Singleline);

            if (findRegEx.IsMatch(content))
            {
                return;
            }

            Regex replaceRegEx = new Regex(@"<ItemGroup>", RegexOptions.RightToLeft);

            string relativeDepth = "";

            for (int i = 0; i < depth; i++)
            {
                relativeDepth += ONE_UP;
            }


            string newContent = replaceRegEx.Replace(content, $@"<ItemGroup>
    <Compile Include=""{relativeDepth}CommonAssemblyInfo.cs"">
      <Link>Properties\CommonAssemblyInfo.cs</Link>
    </Compile>", 1);

            await FileOperationHelper.WriteAllTextAsync(filePath, newContent);
        }

        private static async Task UpdateAssemblyInfoFileAsync(string assemblyInfoPath, string commonAssemblyPath)
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