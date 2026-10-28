using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Tables whose declared date column stops some years before the run, and empty tables: candidates for
    /// tables no longer in use. From the shared profile read, no extra queries. One result across tables.
    /// </summary>
    public sealed class TablesWithNoRecentRowsCheck : IAutomaticCheck
    {
        /// <summary>
        /// A table is listed when its latest year is at least this many years before the run's. The profile
        /// keeps years only, so a 2026 run lists 2024 and earlier.
        /// </summary>
        internal const int YearsOld = 2;

        private const string Description =
            "Tables whose declared date column's latest year is 2 or more years before this run, and empty tables: candidates for tables " +
            "no longer written, which may not need migrating. A date far in the future (see Implausible dates) raises the latest year " +
            "and can hide such a table. Tables with no declared date column are judged only when empty.";

        public string Name => "Tables with no recent rows";

        public bool AcceptsTableOverrides => false;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var result = new CheckResult
            {
                Name = Name,
                Description = Description,
                Kind = ResultKind.DesignObservation,
                Severity = Severity.Info,
                Status = CheckStatus.NoProblemsFound
            };

            var runYear = DateTime.Now.Year;
            var found = new List<(TableRef Table, string DateColumn, int? Latest, long Rows)>();
            var notJudged = 0;
            var notRead = 0;
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).ToList();
            foreach (var table in tables)
            {
                var profile = context.Profile(table);
                if (profile.Error != null)
                {
                    notRead++;
                    continue;
                }
                var dated = context.Plan.DateColumns.TryGetValue(table, out var column);
                var latest = dated ? profile.Columns.Find(c => c.Column == column!.Name)?.LatestYear : null;
                if (profile.Rows == 0 || IsOld(latest, runYear))
                {
                    found.Add((table, dated ? column!.Name : "-", latest, profile.Rows));
                }
                else if (latest == null)
                {
                    notJudged++;
                }
            }

            if (found.Count > 0)
            {
                result.Status = CheckStatus.ProblemsFound;
                var rows = found
                    .OrderBy(f => f.Rows > 0).ThenBy(f => f.Latest).ThenBy(f => f.Table.Name, StringComparer.Ordinal)
                    .Select(f => new List<string>
                    {
                        f.Table.Name, f.DateColumn, f.Latest is { } latest ? latest.ToString(CultureInfo.InvariantCulture) : "empty", Count(f.Rows)
                    })
                    .ToList();
                result.Grids.Add(new ResultGrid("Tables", ["Table"], ["Date column", "Latest year", "Rows"], rows, rows.Count));
            }
            else
            {
                result.Details.Add($"Checked {Count(tables.Count)} tables; none found.");
            }
            if (notJudged > 0)
            {
                result.Details.Add($"Not judged, with no declared date column or no dates in it: {Count(notJudged)} tables " +
                    "(declare one under dateColumns to include them).");
            }
            if (notRead > 0)
            {
                result.Details.Add($"Not judged, since their profile read failed: {Count(notRead)} tables.");
            }
            return [result];
        }

        internal static bool IsOld(int? latestYear, int runYear) => latestYear <= runYear - YearsOld;

        private static string Count(long n) => n.ToString("N0", CultureInfo.InvariantCulture);
    }
}
