using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Columns of the types Microsoft has deprecated (text, ntext, image), with what replaces each.
    /// Metadata only.
    /// </summary>
    public sealed class DeprecatedTypesCheck : IAutomaticCheck
    {
        private const string Description = "Columns of deprecated types, which future SQL Server versions will drop and most string functions do not accept.";

        private static readonly Dictionary<string, string> Replacements = new(StringComparer.Ordinal)
        {
            ["text"] = "varchar(max)",
            ["ntext"] = "nvarchar(max)",
            ["image"] = "varbinary(max)"
        };

        public string Name => "Deprecated types";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
            var results = new List<CheckResult>();
            foreach (var table in tables)
            {
                var result = AutomaticResult.For(context, Name, Description, ResultKind.DesignObservation, Severity.Info, table.Name);
                var found = table.Columns.Where(c => Replacements.ContainsKey(c.BaseType)).ToList();
                if (result.Status != CheckStatus.Skipped && found.Count > 0)
                {
                    result.Status = CheckStatus.ProblemsFound;
                    result.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type", "Replace with"],
                        found.Select(c => new List<string> { c.Name, c.BaseType, Replacements[c.BaseType] }).ToList(), found.Count));
                }
                results.Add(result);
            }
            return AutomaticResult.OnlyFindings(results, Name, Description, Severity.Info,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found", ResultKind.DesignObservation);
        }
    }
}
