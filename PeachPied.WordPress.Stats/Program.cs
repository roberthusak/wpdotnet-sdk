using Pchp.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using PeachPied.WordPress.Standard;

namespace PeachPied.WordPress.Stats
{
    class Program
    {
        static void Main(string[] args)
        {
            string solutionDir = Path.GetFullPath("..");
            string wpDir = $"{solutionDir}/wordpress";

            PrintCompilationData(args, wpDir);
            PrintRuntimeData(args, solutionDir);
        }

        private static void PrintCompilationData(string[] configurations, string wpDir)
        {
            var headers =
                new[]
                {
                    "Configuration",
                    "Routines",
                    "Functions",
                    "Specializations",
                    "Routine call sites",
                    "All function call sites",
                    "Library function call sites",
                    "Ambiguous source function call sites",
                    "Branched source function call sites",
                    "Original source function call sites",
                    "Specialized source function call sites"
                };

            var results = new string[configurations.Length][];
            for (int i = 0; i < configurations.Length; i++)
            {
                string configuration = configurations[i];
                var assembly =
                    Assembly.LoadFrom($"{wpDir}/bin/{configuration}/netstandard2.0/Peachpied.WordPress.{configuration}.dll");
                var compilationCounters = assembly.GetCustomAttribute<CompilationCountersAttribute>();

                results[i] =
                    new[]
                    {
                        configuration,
                        compilationCounters.Routines.ToString(),
                        compilationCounters.GlobalFunctions.ToString(),
                        compilationCounters.Specializations.ToString(),
                        compilationCounters.RoutineCalls.ToString(),
                        compilationCounters.FunctionCalls.ToString(),
                        compilationCounters.LibraryFunctionCalls.ToString(),
                        compilationCounters.AmbiguousSourceFunctionCalls.ToString(),
                        compilationCounters.BranchedSourceFunctionCalls.ToString(),
                        compilationCounters.OriginalSourceFunctionCalls.ToString(),
                        compilationCounters.SpecializedSourceFunctionCalls.ToString()
                    };
            }

            Console.WriteLine("Compilation statistics:");
            Console.WriteLine();

            var (table1, table2) = new Table(headers, results).Split(1, 6);
            table1.Print(Console.Out);
            Console.WriteLine();
            table2.Print(Console.Out);
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void PrintRuntimeData(string[] configurations, string solutionDir)
        {
            string statsRunnerDir = Path.Combine(solutionDir, "PeachPied.WordPress.StatsRunner");

            var headers =
                new[]
                {
                    "Configuration",
                    "Routine calls",
                    "Function calls",
                    "Original source function calls",
                    "Specialized source function calls",
                    "Branched call checks",
                    "Branched call original selects",
                    "Branched call specialized selects"
                };

            var results = new string[configurations.Length][];
            for (int i = 0; i < configurations.Length; i++)
            {
                string configuration = configurations[i];
                var processInfo = new ProcessStartInfo("dotnet", $"run -c Release --no-build -- {configuration}")
                {
                    WorkingDirectory = statsRunnerDir,
                    RedirectStandardOutput = true,
                };

                var process = Process.Start(processInfo);
                process.WaitForExit();

                var data =
                    process.StandardOutput
                        .ReadToEnd()
                        .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                var line = new string[data.Length + 1];
                line[0] = configuration;
                Array.Copy(data, 0, line, 1, data.Length);

                results[i] = line;
            }

            Console.WriteLine("Runtime statistics:");
            Console.WriteLine();

            var (table1, table2) = new Table(headers, results).Split(1, 4);
            table1.Print(Console.Out);
            Console.WriteLine();
            table2.Print(Console.Out);
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
