using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonAssemblyInjector
{
    public static class FileOperationHelper
    {
        public static async Task WriteAllTextAsync(string path, string content)
        {
            await Task.Factory.StartNew(() => { File.WriteAllText(path, content); });
        }

        public static async Task<string> ReadAllTextAsync(string path)
        {
            string result = await Task.Factory.StartNew(() => File.ReadAllText(path));
            return result;
        }

        public static async Task<IEnumerable<string>> GetFilesAsync(string path, string searchPattern,
            SearchOption searchOption)
        {
            return await Task.Factory.StartNew(() => Directory.GetFiles(path, searchPattern, searchOption).ToList());
        }

        public static async Task<int> GetPathDepthAsync(string path)
        {
            return await Task.Factory.StartNew(() => path.Split("\\").Length);
        }

        public static async Task<int> GetFileDepthDiffAsync(string path, string file)
        {
            return await Task.Factory.StartNew(() => FileOperationHelper.GetPathDepthAsync(file).Result - FileOperationHelper.GetPathDepthAsync(path).Result - 1);
        }
    }
}
