using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Columns in one table whose names differ only by a number (Choice1, Choice2, Choice3, or
    /// Phone1Type, Phone2Type): candidates for a separate table with one row per item. Metadata only.
    /// </summary>
    public sealed class RepeatingColumnGroupsCheck : IAutomaticCheck
    {
        private const string Description = "Columns whose names differ only by a number: candidates for a separate table with one row per item.";

        private static readonly Regex Digits = new("[0-9]+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        public string Name => "Repeating column groups";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
            var results = new List<CheckResult>();
            foreach (var table in tables)
            {
                var result = AutomaticResult.For(context, Name, Description, ResultKind.DesignObservation, Severity.Info, table.Name);
                if (result.Status != CheckStatus.Skipped)
                {
                    // Each run of digits becomes #, so Choice1 and Choice2 share the pattern Choice#. A group
                    // needs at least two different numbers.
                    var groups = table.Columns
                        .Where(c => Digits.IsMatch(c.Name))
                        .GroupBy(c => Digits.Replace(c.Name, "#"), StringComparer.OrdinalIgnoreCase)
                        .Where(g => g.Count() >= 2)
                        .ToList();
                    if (groups.Count > 0)
                    {
                        result.Status = CheckStatus.ProblemsFound;
                        result.Grids.Add(new ResultGrid("Groups", ["Pattern"], ["Columns"],
                            groups.Select(g => new List<string> { g.Key, string.Join(", ", g.Select(c => c.Name)) }).ToList(), groups.Count));
                    }
                }
                results.Add(result);
            }
            return AutomaticResult.OnlyFindings(results, Name, Description, Severity.Info,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found", ResultKind.DesignObservation);
        }
    }
}
