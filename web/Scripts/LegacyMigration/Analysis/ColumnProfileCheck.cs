using System;
using System.Collections.Generic;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Counts for every column of every table, from one read per table: NULLs, blanks, distinct values,
    /// lowest and highest number, earliest and latest year, longest text. Never text values. The report
    /// lists columns that are always NULL, always blank, or hold one value; results.json has the rest.
    ///
    /// Distinct counts follow each column's collation, so under a case-insensitive one ABC and abc count
    /// once. The read is TableProfile, which the checks for spaces, dates, and characters share.
    /// </summary>
    public sealed class ColumnProfileCheck : IAutomaticCheck
    {
        public string Name => "Column profile";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            foreach (var table in context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                var result = AutomaticResult.For(context, Name, null, ResultKind.Context, Severity.Info, table.Name);
                if (result.Status == CheckStatus.Skipped)
                {
                    yield return result;
                    continue;
                }

                var profile = context.Profile(table);
                Console.WriteLine($"  {table.Name}: {profile.Elapsed.TotalSeconds:0.0}s");
                if (profile.Error != null)
                {
                    result.Status = CheckStatus.FailedToRun;
                    result.Message = $"Failed to run: {profile.Error}";
                    yield return result;
                    continue;
                }

                Summarize(profile, result);
                yield return result;
            }
        }

        private static void Summarize(TableProfile profile, CheckResult result)
        {
            var rows = profile.Rows;
            var profiles = profile.Columns;
            result.ColumnProfiles = profiles;
            result.Counts.Add(new ResultCount("Rows", rows));

            if (rows == 0)
            {
                result.Details.Add("Empty table.");
                return;
            }

            AddList(result, "Always NULL", profiles.Where(p => p.Nulls == rows));
            AddList(result, "Always blank (or NULL)", profiles.Where(p => p.Blanks > 0 && p.Nulls + p.Blanks == rows));
            AddList(result, "Only one value, ignoring NULLs", profiles.Where(p => p.Distinct == 1 && p.Nulls < rows && !(p.Blanks > 0 && p.Nulls + p.Blanks == rows)));
            AddList(result, "Not profiled", profiles.Where(p => p.NotProfiled != null), p => $"{p.Column} ({p.NotProfiled})");
        }

        private static void AddList(CheckResult result, string label, IEnumerable<ColumnProfile> profiles, Func<ColumnProfile, string>? describe = null)
        {
            var names = profiles.Select(describe ?? (p => p.Column)).ToList();
            if (names.Count > 0)
            {
                result.Details.Add($"{label} ({names.Count}): {string.Join(", ", names)}");
            }
        }
    }
}
