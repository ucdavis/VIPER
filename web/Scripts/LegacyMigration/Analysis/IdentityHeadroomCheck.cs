using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// How much of each identity column's range is used, from the export's last identity value: no table
    /// reads. A table past WarningPercent gets a warning; every identity column is listed, most used first,
    /// as context.
    /// </summary>
    public sealed class IdentityHeadroomCheck : IAutomaticCheck
    {
        private const decimal WarningPercent = 80;
        private const int ListLimit = 10;

        private const string Description = "How much of each identity column's range is used. Inserts fail once it runs out.";

        public string Name => "Identity headroom";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var columns = context.Plan.Schema.Tables
                .Where(t => t.GetsAutomaticChecks)
                .SelectMany(t => t.Columns.Where(c => c.Summary.Identity != null))
                .Select(c => (Column: c, Used: PercentUsed(c)))
                .Where(c => c.Used.HasValue)
                .Select(c => (c.Column, Used: c.Used.GetValueOrDefault()))
                .OrderByDescending(c => c.Used)
                .ToList();

            var results = new List<CheckResult>();
            foreach (var (column, used) in columns.Where(c => c.Used >= WarningPercent))
            {
                var result = AutomaticResult.For(context, Name, Description, ResultKind.Problem, Severity.Warning, column.Table.Name);
                if (result.Status != CheckStatus.Skipped)
                {
                    result.Status = CheckStatus.ProblemsFound;
                    result.Details.Add($"{column.Name} ({column.Summary.Type}): last value {column.Summary.Identity!.LastValue}, {used:0.#}% of its range used.");
                }
                results.Add(result);
            }
            foreach (var finding in AutomaticResult.OnlyFindings(results, Name, Description, Severity.Warning,
                         $"{columns.Count.ToString("N0", CultureInfo.InvariantCulture)} identity columns; none over {WarningPercent}% used"))
            {
                yield return finding;
            }

            if (columns.Count > 0)
            {
                var overview = new CheckResult
                {
                    Name = Name,
                    Description = Description,
                    Kind = ResultKind.Context,
                    Severity = Severity.Info,
                    Status = CheckStatus.NoProblemsFound
                };
                overview.Grids.Add(new ResultGrid("Most used", ["Table", "Column"], ["Type", "Last value", "Used"],
                    columns.Take(ListLimit).Select(c => new List<string>
                    {
                        c.Column.Table.Name, c.Column.Name, c.Column.Summary.Type, c.Column.Summary.Identity!.LastValue!,
                        $"{c.Used.ToString("0.#", CultureInfo.InvariantCulture)}%"
                    }).ToList(), columns.Count));
                yield return overview;
            }
        }

        /// <summary>
        /// The share of the range between the seed and the type's limit, in the direction of the
        /// increment, that the last value has used. Null when no row was ever inserted or the type is not
        /// a whole-number type.
        /// </summary>
        private static decimal? PercentUsed(ColumnRef column)
        {
            var identity = column.Summary.Identity!;
            if (identity.LastValue == null || Limits(column) is not { } limits
                || !decimal.TryParse(identity.Seed, NumberStyles.Number, CultureInfo.InvariantCulture, out var seed)
                || !decimal.TryParse(identity.Increment, NumberStyles.Number, CultureInfo.InvariantCulture, out var increment)
                || !decimal.TryParse(identity.LastValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var last))
            {
                return null;
            }

            var (range, usedSoFar) = increment >= 0 ? (limits.Max - seed, last - seed) : (seed - limits.Min, seed - last);
            return range > 0 ? Math.Max(0, usedSoFar) / range * 100 : 100;
        }

        private static (decimal Min, decimal Max)? Limits(ColumnRef column)
        {
            switch (column.BaseType)
            {
                case "tinyint":
                    return (byte.MinValue, byte.MaxValue);
                case "smallint":
                    return (short.MinValue, short.MaxValue);
                case "int":
                    return (int.MinValue, int.MaxValue);
                case "bigint":
                    return (long.MinValue, long.MaxValue);
                case "decimal" or "numeric":
                    // numeric(p, 0): p digits. Precision is written into the type, as in numeric(18, 0).
                    var open = column.Summary.Type.IndexOf('(');
                    var precision = open >= 0 ? column.Summary.Type[(open + 1)..].Split(',', ')')[0].Trim() : "18";
                    if (!int.TryParse(precision, NumberStyles.None, CultureInfo.InvariantCulture, out var digits) || digits > 28)
                    {
                        return null;
                    }
                    var max = Enumerable.Repeat(10m, digits).Aggregate(1m, (product, ten) => product * ten) - 1;
                    return (-max, max);
                default:
                    return null;
            }
        }
    }
}
