using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Pchp.Core;
using StatsCommon;

#nullable enable

namespace PeachPied.PhpBenchmarks.Runner
{
    class Program
    {
        private const int WarmupRuns = 4;
        private const int MeasuredRuns = 8;
        private const string RunSeparator = "<RunEnd>";

        static void Main(string[] args)
        {
            var configurations = args;

            string solutionDir = Path.GetFullPath("..");
            string benchDir = $"{solutionDir}/bench";

            var results =
                configurations
                    .Select(c => (c, RunBenchmarks(benchDir, c)))
                    .ToArray();
            var table = ProcessResults(results);

            var (table1, table2) = table.Split(1, 10);
            Console.WriteLine();
            table1.Print(Console.Out);
            Console.WriteLine();
            Console.WriteLine();
            table2.Print(Console.Out);
            Console.WriteLine();
        }

        private static string RunBenchmarks(string benchDir, string configuration)
        {
            var processInfo = new ProcessStartInfo(
                "dotnet",
                $"run -c {configuration} --no-build -- {WarmupRuns + MeasuredRuns}")
            {
                WorkingDirectory = benchDir,
                RedirectStandardOutput = true,
            };

            var process = Process.Start(processInfo);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private static Table ProcessResults((string configuration, string output)[] results)
        {
            var headers =
                new[] { "Configuration" }
                    .Concat(
                        SplitToLines(results[0].output)
                            .TakeWhile(line => line != RunSeparator)
                            .Select(SelectFirstColumn)
                            .OrderBy(s => s)
                    )
                    .ToArray();

            var rows =
                results
                    .Select(result =>
                        new[] { result.configuration }
                            .Concat(
                                result.output
                                    .Split(RunSeparator, StringSplitOptions.RemoveEmptyEntries)
                                    .Skip(WarmupRuns)
                                    .SelectMany(SplitToLines)
                                    .Select(line => (SelectFirstColumn(line), double.Parse(SelectLastColumn(line))))
                                    .GroupBy(record => record.Item1)
                                    .Select(group => (group.Key, group.Average(r => r.Item2)))
                                    .OrderBy(record => record.Item1)
                                    .Select(record => record.Item2.ToString("F5"))
                            )
                            .ToArray()
                    )
                    .ToArray();

            return new Table(headers, rows);

            string[] SplitToLines(string text) => text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string SelectFirstColumn(string line) => line.Substring(0, line.IndexOf(' '));
            
            string SelectLastColumn(string line) => line.Substring(line.LastIndexOf(' ') + 1);
        }
    }
}
