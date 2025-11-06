using System;
using System.Linq;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Entry point for Effort System migration and data scripts
    /// Routes to different operations based on command line args:
    /// - analysis: Run data analysis queries
    /// - remediation: Run data remediation scripts
    /// - schema-export: Export legacy schema documentation
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            var command = args[0].ToLowerInvariant();
            var commandArgs = args.Skip(1).ToArray();

            switch (command)
            {
                case "analysis":
                    EffortDataAnalysis.Run(commandArgs);
                    break;

                case "remediation":
                    EffortDataRemediation.Run(commandArgs);
                    break;

                case "schema-export":
                    EffortSchemaExport.Run(commandArgs);
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    break;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Effort Migration Toolkit");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  analysis         Run data analysis queries against legacy database");
            Console.WriteLine("  remediation      Run data remediation scripts");
            Console.WriteLine("  schema-export    Export legacy schema documentation");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- analysis");
            Console.WriteLine("  dotnet run -- remediation --table tblPerson");
            Console.WriteLine("  dotnet run -- schema-export --output ./docs/schema.md");
        }
    }
}
