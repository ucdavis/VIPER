using System.Collections.Generic;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Reports the columns the schema export flagged as holding a person (see PersonIdRules), one result
    /// per table or view: both those whose name or foreign key says the ID type and those left for review.
    /// Shape comes from the shared profile read, so it costs no query of its own; views are not read.
    /// </summary>
    public sealed class PersonColumnsCheck : IAutomaticCheck
    {
        private const string Description =
            "Columns that hold a person. ID type is the type the column's name or foreign key says it holds; " +
            "\"not stated\" means the name says who made a change but not with which ID, so review what it holds. " +
            "Shape counts the non-blank values that are not numbers: MothraIds, IAM IDs, and PIDMs are numbers, so any others are " +
            "another kind of ID (such as LoginIds), placeholders, or something else, and a declared identifiesOnePerson " +
            "check says which. Views are not read, since a view re-reads its tables and often other databases; " +
            "a declared identifiesOnePerson check on a view's column checks its values.";

        public string Name => "Person columns";

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            foreach (var group in context.Plan.Summary.PersonIdColumns.GroupBy(c => c.Object))
            {
                var table = context.Plan.Schema.FindTableOrView(group.Key);
                if (table is { GetsAutomaticChecks: false })
                {
                    continue;
                }

                var result = AutomaticResult.For(context, Name, Description, ResultKind.DesignObservation, Severity.Info, group.Key);
                if (result.Status != CheckStatus.Skipped)
                {
                    var profile = table is { IsView: false } ? context.Profile(table) : null;
                    var order = table?.Columns.Select((c, i) => (c.Name, i)).ToDictionary(c => c.Name, c => c.i) ?? [];
                    var rows = group
                        .OrderBy(c => order.GetValueOrDefault(c.Column))
                        .Select(c => new List<string> { c.Column, c.Type, c.IdType ?? "not stated", c.Reason, Shape(profile, c.Column) })
                        .ToList();
                    result.Status = CheckStatus.ProblemsFound;
                    result.Grids.Add(new ResultGrid("Columns", ["Column"], ["Type", "ID type", "Why", "Shape"], rows, rows.Count));
                }
                yield return result;
            }
        }

        /// <summary>
        /// "all numbers", or how many non-blank values are not. Only text columns are counted: a number
        /// column holds only numbers already.
        /// </summary>
        private static string Shape(TableProfile? profile, string column)
        {
            if (profile == null)
            {
                return "view, not read";
            }
            if (profile.Error != null)
            {
                return "not read (profile failed)";
            }

            var counts = profile.Columns.First(c => c.Column == column);
            var numbers = profile.Extras[column].Numbers;
            if (numbers == null)
            {
                return "not text";
            }
            var values = profile.Rows - (counts.Nulls ?? 0) - (counts.Blanks ?? 0);
            if (values == 0)
            {
                return "no values";
            }
            return numbers == values ? "all numbers" : $"{values - numbers:N0} of {values:N0} not numbers";
        }
    }
}
