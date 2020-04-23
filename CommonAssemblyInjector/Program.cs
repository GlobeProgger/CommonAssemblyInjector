using System;
using System.Threading.Tasks;

namespace CommonAssemblyInjector
{
    class Program
    {
        private const string SOL_DIR = "/solDir:";
        private const string PATH = "/path:";
        private const string VERSION = "/version:";
        private const string IGNORE = "/ignore:";

        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                PrintUsage();
                return;
            }

            Injector injector = new Injector();

            foreach (string arg in args)
            {
                if (arg.StartsWith(SOL_DIR))
                {
                    injector.SolutionDir = arg.Substring(SOL_DIR.Length).StripQuotes();
                }

                if (arg.StartsWith(PATH))
                {
                    injector.CommonAssemblyInfoPath = arg.Substring(PATH.Length).StripQuotes();
                }

                if (arg.StartsWith(VERSION))
                {
                    injector.TargetVersion = arg.Substring(VERSION.Length).StripQuotes();
                }

                if (arg.StartsWith(IGNORE))
                {
                    injector.IgnoreDirs = arg.Substring(IGNORE.Length).StripQuotes().Split(',');
                }
            }

            await injector.TryAddCommonAssemblyToProjects();
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
