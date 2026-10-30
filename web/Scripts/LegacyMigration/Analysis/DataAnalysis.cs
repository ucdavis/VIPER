using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Runs the data quality analysis against a legacy database: the automatic checks, plus the checks
    /// declared in Areas/{connection}/analysis.json, or Areas/{connection}/{project}/analysis.json for one
    /// project of a database holding several. Reads table data and never changes it; its report holds only
    /// counts, row keys, and dates (see the output and privacy rules in docs/ANALYSIS-REFERENCE.md).
    ///
    /// Refreshes the schema export first and checks every declared name against it. Output:
    /// Output/{connection}/analysis/{environment}/ (plus /{project}), outside the export's folder so a
    /// re-export does not delete earlier results.
    /// </summary>
    public static class DataAnalysis
    {
        public static int Run(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("LEGACY DATABASE DATA QUALITY ANALYSIS");
            Console.WriteLine("================================================================================");
            Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            var runTimer = Stopwatch.StartNew();
            Console.WriteLine();

            try
            {
                var connectionName = LegacyMigrationScriptHelper.ValidateConnectionName(
                    LegacyMigrationScriptHelper.GetRequiredArg(args, "connection"));
                var project = LegacyMigrationScriptHelper.ValidateProjectName(LegacyMigrationScriptHelper.GetOptionalArg(args, "project"));
                var environment = LegacyMigrationScriptHelper.RequireEnvironment();
                var toolRoot = LegacyMigrationScriptHelper.GetToolRoot();

                // Unreadable JSON is caught before connecting; names can only be checked once the export
                // below has refreshed them.
                var (declarationsPath, outputDir) = LegacyMigrationScriptHelper.AnalysisPaths(toolRoot, connectionName, environment, project);
                var hasDeclarations = File.Exists(declarationsPath);
                if (project != null && !hasDeclarations)
                {
                    throw new ArgumentException($"Project {project} has no declarations file at {declarationsPath}.");
                }
                var declarations = Declarations.Load(declarationsPath);

                var configuration = LegacyMigrationScriptHelper.LoadConfiguration(environment);
                var connectionString = LegacyMigrationScriptHelper.GetConnectionString(configuration, connectionName, environment);

                Console.WriteLine();
                Console.WriteLine($"  Connection:   {connectionName} ({environment})");
                if (project != null)
                {
                    Console.WriteLine($"  Project:      {project}");
                }
                Console.WriteLine($"  Database:     {LegacyMigrationScriptHelper.GetServerAndDatabase(connectionString)}");
                Console.WriteLine($"  Declarations: {(hasDeclarations ? declarationsPath : "(none: automatic checks only)")}");
                Console.WriteLine($"  Output:       {outputDir}");
                Console.WriteLine();

                Console.WriteLine("Refreshing the schema export...");
                var summary = SchemaExport.Export(connectionString, connectionName, environment);
                Console.WriteLine();
                var plan = AnalysisPlan.Bind(summary, declarations);

                using var database = AnalysisDatabase.Open(connectionString);
                using var context = new AnalysisContext(plan, database,
                    name => AnalysisDatabase.Open(LegacyMigrationScriptHelper.GetConnectionString(configuration, name, environment)));
                var timer = Stopwatch.StartNew();
                var results = RunChecks(context);
                var run = new AnalysisRun(connectionName, project, environment, summary.Server, summary.Database, summary.GeneratedAt,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), $"{timer.Elapsed.TotalSeconds:0.0}s",
                    hasDeclarations ? Path.GetRelativePath(toolRoot, declarationsPath) : null, declarations.Include,
                    ObjectCoverage.Of(plan.Schema.Tables), ObjectCoverage.Of(plan.Schema.Views), results,
                    plan.Schema.MaskedColumns.Select(MaskedColumn.Of).ToList());

                // The last run's results.json, read before this run overwrites it.
                var resultsPath = Path.Join(outputDir, "results.json");
                var (previous, problem) = RunComparison.Read(resultsPath);
                var changes = previous != null ? RunComparison.Lines(previous, RunComparison.Snapshot(run)) : [$"({problem})"];
                var report = AnalysisReport.ToText(run, changes);
                var json = AnalysisReport.ToJson(run);

                // Printed before the files are written, so a file that cannot be written (open elsewhere,
                // say) does not cost a finished run its report.
                Console.Write(report);
                Console.WriteLine();

                // Every run is also kept under history, so any earlier run can be looked back at.
                var historyDir = Path.Join(outputDir, "history");
                Directory.CreateDirectory(historyDir);
                LegacyMigrationScriptHelper.WriteCrlfFile(Path.Join(outputDir, "report.txt"), report);
                LegacyMigrationScriptHelper.WriteCrlfFile(resultsPath, json);
                LegacyMigrationScriptHelper.WriteCrlfFile(Path.Join(historyDir, HistoryFileName(DateTime.Now)), json);
                Console.WriteLine($"Report saved to: {outputDir}");
                LegacyMigrationScriptHelper.WriteFinished(runTimer);
                return 0;
            }
            catch (DeclarationException ex)
            {
                LegacyMigrationScriptHelper.WriteError("DECLARATIONS ERROR", "nothing was run. Fix these and run again:");
                foreach (var error in ex.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            catch (ArgumentException ex)
            {
                LegacyMigrationScriptHelper.WriteError("ARGUMENT ERROR", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                LegacyMigrationScriptHelper.WriteError("CONFIGURATION ERROR", ex.Message);
            }
            catch (SqlException ex)
            {
                LegacyMigrationScriptHelper.WriteError("DATABASE ERROR", $"{ex.Message} (error {ex.Number})");
            }
            catch (UnauthorizedAccessException ex)
            {
                LegacyMigrationScriptHelper.WriteError("ACCESS DENIED", ex.Message);
            }
            catch (IOException ex)
            {
                LegacyMigrationScriptHelper.WriteError("FILE I/O ERROR", ex.Message);
            }

            return 1;
        }

        /// <summary>
        /// Down to the millisecond, so two runs finishing in the same second keep both copies.
        /// </summary>
        internal static string HistoryFileName(DateTime runTime) => $"results-{runTime:yyyy-MM-dd-HHmmss-fff}.json";

        /// <summary>
        /// A check that fails is reported as failed to run and the rest carry on, so one bad query does
        /// not cost the whole run.
        /// </summary>
        private static List<CheckResult> RunChecks(AnalysisContext context)
        {
            var results = new List<CheckResult>();

            foreach (var check in CheckRegistry.Automatic)
            {
                Console.WriteLine($"Running {check.Name}...");
                results.AddRange(RunOne(context, () => check.Run(context), check.Name, null, Severity.Warning, null));
            }

            foreach (var check in context.Plan.DeclaredChecks)
            {
                Console.WriteLine($"Running {check.Name} on {check.Subject}...");
                var excluded = check.Tables.Where(t => t.IsExcluded).Select(t => t.Name).ToList();
                if (excluded.Count > 0)
                {
                    results.Add(new CheckResult
                    {
                        Name = check.Name,
                        Description = check.Description,
                        Kind = ResultKind.Problem,
                        Severity = check.Severity,
                        Table = check.Subject,
                        Status = CheckStatus.Skipped,
                        Message = $"Skipped: reads an excluded table or view ({string.Join(", ", excluded)})."
                    });
                    continue;
                }

                results.AddRange(RunOne(context, () => check.Run(context), check.Name, check.Description, check.Severity, check.Subject));
            }

            return results;
        }

        /// <summary>
        /// One check's results, or one failed result whatever it throws, with the message's quoted values
        /// described by shape.
        /// </summary>
        internal static List<CheckResult> RunOne(AnalysisContext context, Func<IEnumerable<CheckResult>> run,
            string name, string? description, Severity severity, string? subject)
        {
            try
            {
                return run().ToList();
            }
            catch (SqlException ex)
            {
                return [Failed(name, description, severity, context.ErrorText(ex), subject)];
            }
            catch (InvalidOperationException ex)
            {
                // A lookup connection whose connection string is missing.
                return [Failed(name, description, severity, context.ErrorText(ex), subject)];
            }
            // A fault in one check must not cost the results of the rest of the run, but a failure of the
            // process itself must still end it. (Stack overflow and thread abort cannot be caught at all.)
#pragma warning disable CA1031
            catch (Exception ex) when (ex is not (OutOfMemoryException or AccessViolationException or BadImageFormatException or InvalidProgramException))
#pragma warning restore CA1031
            {
                return [Failed(name, description, severity, BugMessage(context, ex), subject)];
            }
        }

        /// <summary>
        /// A fault in the check's own code rather than the data or the database: the exception's type says what
        /// kind, and the stack's first frame says where to look.
        /// </summary>
        private static string BugMessage(AnalysisContext context, Exception ex) =>
            $"{ex.GetType().Name}: {context.ErrorText(ex)} (a fault in the check, at {ex.StackTrace?.Split('\n').FirstOrDefault()?.Trim() ?? "an unknown place"})";

        private static CheckResult Failed(string name, string? description, Severity severity, string error, string? subject = null) => new()
        {
            Name = name,
            Description = description,
            Kind = ResultKind.Problem,
            Severity = severity,
            Table = subject,
            Status = CheckStatus.FailedToRun,
            Message = $"Failed to run: {error}"
        };
    }
}
