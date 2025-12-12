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
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            var command = args[0].ToLowerInvariant();
            var commandArgs = args.Skip(1).ToArray();

            switch (command)
            {
                case "analysis":
                    EffortDataAnalysis.Run(commandArgs);
                    return 0;

                case "remediation":
                    EffortDataRemediation.Run(commandArgs);
                    return 0;

                case "schema-export":
                    EffortSchemaExport.Run(commandArgs);
                    return 0;

                case "create-database":
                    return CreateEffortDatabase.Run(commandArgs);

                case "migrate-data":
                    MigrateEffortData.Run(commandArgs);
                    return 0;

                case "create-shadow":
                    return CreateEffortShadow.Run(commandArgs);

                case "create-reporting-procedures":
                    CreateEffortReportingProcedures.Run(commandArgs);
                    return 0;

                case "verify-shadow":
                    return VerifyShadowProcedures.Run(commandArgs);

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    return 1;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Effort Migration Toolkit");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  analysis                      Run data analysis queries against legacy database");
            Console.WriteLine("  remediation                   Run data remediation scripts");
            Console.WriteLine("  schema-export                 Export legacy schema documentation");
            Console.WriteLine("  create-database               Create modernized Effort schema in VIPER database");
            Console.WriteLine("  migrate-data                  Migrate data from legacy Efforts database");
            Console.WriteLine("  create-shadow                 Create EffortShadow compatibility schema");
            Console.WriteLine("  create-reporting-procedures   Create modernized reporting stored procedures");
            Console.WriteLine("  verify-shadow                 Verify shadow schema procedures match legacy");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- analysis");
            Console.WriteLine("  dotnet run -- remediation --table tblPerson");
            Console.WriteLine("  dotnet run -- schema-export");
            Console.WriteLine("  dotnet run -- create-database --apply");
            Console.WriteLine("  dotnet run -- migrate-data --apply");
            Console.WriteLine("  dotnet run -- create-shadow --apply");
            Console.WriteLine("  dotnet run -- create-reporting-procedures --apply");
            Console.WriteLine("  dotnet run -- verify-shadow");
        }
    }
}
