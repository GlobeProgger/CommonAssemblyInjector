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

        public static string SolutionDir;
        public static string CommonAssemblyInfoPath;
        public static string TargetVersion;

        public static async Task TryAddCommonAssemblyToProjects()
        {
            IAsyncEnumerable<string> filePaths =
                GetRelevantProjectFiles(SolutionDir, TargetVersion, CommonAssemblyInfoPath);

            await foreach (string filePath in filePaths)
            {
                string content = await ReadAllTextAsync(filePath);
                int depth = await GetFileDepthDiff(SolutionDir, filePath);

                if (TryAddCommonAssemblyInfoToProjectFiles(content, depth, out content))
                {
                    await WriteAllTextAsync(filePath, content);
                }
            }
        }

        private static async IAsyncEnumerable<string> GetRelevantProjectFiles(string solutionDir, string version,
            string commonAssemblyPath)
        {
            if (!(await GetFilesAsync(solutionDir, FILE_TYPE_ASSEMBLY, SearchOption.AllDirectories) is List<string>
                filesPaths))
            {
                yield break;
            }

            filesPaths.Remove(commonAssemblyPath);

            IAsyncEnumerable<string> filteredFilePaths =
                FilterAssemblyFilesByVersion(filesPaths, version, commonAssemblyPath);

            await foreach (string filteredFilePath in filteredFilePaths)
            {
                string path = Directory.GetParent(Directory.GetParent(filteredFilePath).FullName).FullName;
                yield return Directory.GetFiles(path, FILE_TYPE_PROJECT, SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
            }
        }

        private static async IAsyncEnumerable<string> FilterAssemblyFilesByVersion(IEnumerable<string> filePaths,
            string version, string commonAssemblyPath)
        {
            foreach (string filePath in filePaths)
            {
                string pattern = $@"^\[assembly: AssemblyVersion\(\""{version}\""\)\]";
                string content = await File.ReadAllTextAsync(filePath);

                if (!new Regex(pattern, RegexOptions.Multiline).Match(content).Success)
                {
                    continue;
                }

#pragma warning disable 4014
                UpdateAssemblyInfoFile(filePath, content, commonAssemblyPath);
#pragma warning restore 4014
                yield return filePath;
            }
        }

        private static async Task UpdateAssemblyInfoFile(string assemblyInfoPath, string assemblyInfoContent,
            string commonAssemblyPath)
        {
            string[] commonAssemblyInfoContent = await File.ReadAllLinesAsync(commonAssemblyPath);

            assemblyInfoContent =
                (from line in commonAssemblyInfoContent
                 let pattern = "[assembly: Assembly"
                 where line.StartsWith(pattern)
                 let regex = new Regex(@"(?<line>^\[assembly: Assembly\w+\()", RegexOptions.Multiline)
                 select regex.Match(line)
                    into match
                 where match.Success
                 select match.Captures.FirstOrDefault()?.Value)
                .Aggregate(assemblyInfoContent,
                    (current, s) =>
                        current.Replace($"\r\n{s ?? throw new InvalidOperationException()}", $"\r\n// {s}"));

            await WriteAllTextAsync(assemblyInfoPath, assemblyInfoContent);
        }

        private static bool TryAddCommonAssemblyInfoToProjectFiles(string content, int depth, out string result)
        {
            result = string.Empty;

            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            Regex findRegEx = new Regex(@"<Compile Include=""(..\\)+CommonAssemblyInfo.cs"">",
                RegexOptions.Singleline);

            if (findRegEx.IsMatch(content))
            {
                return false;
            }

            Regex replaceRegEx = new Regex(@"<ItemGroup>", RegexOptions.RightToLeft);

            string relativeDepth = "";

            for (int i = 0; i < depth; i++)
            {
                relativeDepth += ONE_UP;
            }


            result = replaceRegEx.Replace(content, $@"<ItemGroup>
    <Compile Include=""{relativeDepth}CommonAssemblyInfo.cs"">
      <Link>Properties\CommonAssemblyInfo.cs</Link>
    </Compile>", 1);
            return true;
        }

        private static async Task<int> GetFileDepthDiff(string path, string file)
        {
            return await Task.Factory.StartNew(() => GetPathDepth(file).Result - GetPathDepth(path).Result - 1);
        }

        private static async Task<int> GetPathDepth(string path)
        {
            return await Task.Factory.StartNew(() => path.Split("\\").Length);
        }

        private static async Task<IEnumerable<string>> GetFilesAsync(string path, string searchPattern,
            SearchOption searchOption)
        {
            return await Task.Factory.StartNew(() => Directory.GetFiles(path, searchPattern, searchOption).ToList());
        }

        private static async Task<string> ReadAllTextAsync(string path)
        {
            string result = await Task.Factory.StartNew(() => File.ReadAllText(path));
            return result;
        }

        private static async Task WriteAllTextAsync(string path, string content)
        {
            await Task.Factory.StartNew(() => { File.WriteAllText(path, content); });
        }
    }
}