using System;
using System.Linq;

namespace Viper.Areas.ReportBuilder.Scripts
{
    /// <summary>
    /// Entry point for the Report Builder database scripts.
    /// - create-schema: create the [reportbuilder] tables and the empty [reports] schema
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
                case "create-schema":
                    return CreateReportBuilderSchema.Run(commandArgs);

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    return 1;
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Report Builder Scripts");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  create-schema   Create the [reportbuilder] tables and the [reports] schema");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- create-schema           (dry run, rolls back)");
            Console.WriteLine("  dotnet run -- create-schema --apply   (writes permanently)");
        }
    }
}
