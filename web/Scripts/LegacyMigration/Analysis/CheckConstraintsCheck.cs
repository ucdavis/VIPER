using System;
using System.Collections.Generic;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// The CHECK constraints on the tables checked, with their rules and whether SQL Server trusts them,
    /// from the schema export: no table reads. Rules the new schema has to carry over, visible in the
    /// report without opening the export.
    /// </summary>
    public sealed class CheckConstraintsCheck : IAutomaticCheck
    {
        private const string Description =
            "The CHECK constraints on the tables checked: rules the database enforces on every write, which the new schema " +
            "should keep as constraints or validation. A trusted rule holds for every row. An untrusted one was added or " +
            "re-enabled without checking the rows already there, and a disabled one is not enforced at all, so existing rows " +
            "may break either; this report does not check them.";

        public string Name => "Rules the schema enforces";

        public bool AcceptsTableOverrides => false;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var tables = context.Plan.Schema.Tables.Where(t => t.GetsAutomaticChecks).Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
            var constraints = context.Plan.Summary.CheckConstraints
                .Where(c => tables.Contains(c.Table))
                .OrderBy(c => c.Table, StringComparer.Ordinal).ThenBy(c => c.Name, StringComparer.Ordinal)
                .ToList();

            var result = new CheckResult
            {
                Name = Name,
                Description = Description,
                Kind = ResultKind.Context,
                Severity = Severity.Info,
                Status = CheckStatus.NoProblemsFound
            };
            if (constraints.Count == 0)
            {
                result.Message = "No CHECK constraints on the tables checked.";
                return [result];
            }

            result.Grids.Add(new ResultGrid("CHECK constraints", ["Table"], ["Constraint", "Trusted", "Rule"],
                constraints.Select(c => new List<string> { c.Table, c.Name, Trust(c), c.Definition }).ToList(), constraints.Count));
            return [result];
        }

        private static string Trust(CheckConstraintSummary constraint) =>
            constraint.IsDisabled ? "no, disabled" : constraint.IsNotTrusted ? "no" : "yes";
    }
}
