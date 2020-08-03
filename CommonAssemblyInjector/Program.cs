using System;
using System.Threading.Tasks;

namespace CommonAssemblyInjector
{
    internal static class Program
    {
        private const string SolDir = "/solDir:";
        private const string Path = "/path:";
        private const string Version = "/version:";
        private const string Ignore = "/ignore:";

        private static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                PrintUsage();
                return;
            }

            foreach (string arg in args)
            {
                if (arg.StartsWith(SolDir))
                {
                    Injector.SolutionDir = arg.Substring(SolDir.Length).StripQuotes();
                }

                if (arg.StartsWith(Path))
                {
                    Injector.CommonAssemblyInfoPath = arg.Substring(Path.Length).StripQuotes();
                }

                if (arg.StartsWith(Version))
                {
                    Injector.TargetVersion = arg.Substring(Version.Length).StripQuotes();
                }

                if (arg.StartsWith(Ignore))
                {
                    Injector.IgnoreDirs = arg.Substring(Ignore.Length).StripQuotes().Split(',');
                }
            }

            await Injector.TryAddCommonAssemblyToProjects();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: CommonAssemblyInjector "
                              + "[/solDir:<directory_of_solution_to_inject>] "
                              + "[/path:<path_of_CommonAssemblyInfo.cs_File>] "
                              + "[/version:<version_of_assemblies_to_inject(e.g. \"1.0.0.0\")>]"
                              + "[/ignore:<comma_separated_directories_to_ignore>]");
        }
    }
}