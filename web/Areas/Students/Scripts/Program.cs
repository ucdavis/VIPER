using System;
using System.Linq;

namespace Viper.Areas.Students.Scripts
{
    /// <summary>
    /// Entry point for CareerSelection migration and data scripts.
    /// Routes to different operations based on command line args:
    /// - analysis: Run read-only data-quality analysis against the legacy SIS career tables
    /// - migrate-data: Transform and load the legacy data into the students schema
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
                    CareerSelectionDataAnalysis.Run(commandArgs);
                    return 0;

                case "migrate-data":
                    MigrateCareerSelectionData.Run(commandArgs);
                    return 0;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    return 1;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("CareerSelection Migration Toolkit");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  analysis       Run read-only data-quality analysis against the legacy SIS career tables");
            Console.WriteLine("  migrate-data   Migrate legacy career selection data into the students schema");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- analysis");
            Console.WriteLine("  dotnet run -- migrate-data           (dry run, rolls back)");
            Console.WriteLine("  dotnet run -- migrate-data --apply   (writes permanently)");
        }
    }
}
