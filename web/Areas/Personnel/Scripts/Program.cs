using System;
using System.Linq;

namespace Viper.Areas.Personnel.Scripts
{
    /// <summary>
    /// Entry point for PhoneLists migration and data scripts.
    /// Routes to different operations based on command line args:
    /// - analysis: Run read-only data-quality analysis against the legacy PhoneLists database
    /// - migrate-data: Transform and load the legacy data into the phones schema
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
                    PhoneListsDataAnalysis.Run(commandArgs);
                    return 0;

                case "migrate-data":
                    MigratePhoneListsData.Run(commandArgs);
                    return 0;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    return 1;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("PhoneLists Migration Toolkit");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  analysis       Run read-only data-quality analysis against legacy PhoneLists database");
            Console.WriteLine("  migrate-data   Migrate legacy PhoneLists data into the phones schema");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- analysis");
            Console.WriteLine("  dotnet run -- migrate-data           (dry run, rolls back)");
            Console.WriteLine("  dotnet run -- migrate-data --apply   (writes permanently)");
        }
    }
}
