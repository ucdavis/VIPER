using System;
using System.Globalization;
using System.Linq;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.Scripts.LegacyMigration
{
    /// <summary>
    /// Entry point for the legacy migration toolkit, shared by every legacy database migrated into VIPER.
    /// Routes to different operations based on command line args:
    /// - schema-export: Export a legacy database's schema and object definitions (no table data)
    /// - schema-compare: Compare two environments' schema exports (no database connection)
    /// - analysis: Check a legacy database's data quality (reads table data, never changes it; the report holds counts and keys only)
    /// - suggestions: Suggest declarations analysis.json lacks, guessed from the schema export (no table data)
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            // Reports and exports read the same on every machine: 1,234 and 10.6% whatever the Windows
            // settings, so runs from different machines compare. Formatting throughout relies on this.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            var command = args[0].ToLowerInvariant();
            var commandArgs = args.Skip(1).ToArray();

            switch (command)
            {
                case "schema-export":
                    return SchemaExport.Run(commandArgs);

                case "schema-compare":
                    return SchemaCompare.Run(commandArgs);

                case "analysis":
                    return DataAnalysis.Run(commandArgs);

                case "suggestions":
                    return Suggestions.Run(commandArgs);

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    return 1;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Legacy Migration Toolkit");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  RunSchemaExport.bat <ConnectionName> <Development|Test|Production>");
            Console.WriteLine("  RunSchemaCompare.bat <ConnectionName> <Environment> <Environment>");
            Console.WriteLine("  RunAnalysis.bat <ConnectionName> <Development|Test|Production> [Project]");
            Console.WriteLine("  RunSuggestions.bat <ConnectionName> <Development|Test|Production> [Project]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  schema-export --connection <name>                  Export schema and object definitions (no table data)");
            Console.WriteLine("  schema-compare --connection <name> --left <env> --right <env>");
            Console.WriteLine("                                                     Compare two exports (no database connection)");
            Console.WriteLine("  analysis --connection <name> [--project <name>]    Refresh the schema export, then check data quality");
            Console.WriteLine("  suggestions --connection <name> [--project <name>] Refresh the schema export, then suggest what analysis.json lacks");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  RunSchemaExport.bat ClinicalScheduler Test");
            Console.WriteLine("  RunSchemaCompare.bat ClinicalScheduler Test Production");
        }
    }
}
