using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class ValuesMustExistInDeclaration : CheckDeclaration
    {
        public required List<string> Columns { get; init; }
        public required string OtherTable { get; init; }
        public required List<string> OtherColumns { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var other = binder.Object(where, "otherTable", OtherTable);
            var columns = binder.Columns(where, "columns", table, Columns);
            var otherColumns = binder.Columns(where, "otherColumns", other, OtherColumns);
            if (table == null || other == null || columns == null || otherColumns == null)
            {
                return null;
            }
            if (columns.Count != otherColumns.Count)
            {
                binder.Error(where, $"\"columns\" has {columns.Count} and \"otherColumns\" has {otherColumns.Count}; they pair up in order, so they need the same number.");
                return null;
            }

            return new ValuesMustExistInCheck
            {
                Name = $"valuesMustExistIn: {ResultSql.KeyNames(columns)} in {other.Name}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [table, other],
                Columns = columns,
                OtherColumns = otherColumns
            };
        }
    }

    /// <summary>
    /// The values in a table's columns with no matching row in another table, for relationships the
    /// schema never declared. Rows with a NULL in any of the columns are skipped, as SQL Server does for a
    /// foreign key. Text values that match only once spaces are trimmed and case is ignored are reported
    /// separately. A missing value is shown only when its columns' values can be (ResultSql.CanShow);
    /// others are described by shape, with their rows listed by key.
    /// </summary>
    public sealed class ValuesMustExistInCheck : DeclaredCheck
    {
        // SQL Server allows 2,100 parameters per command.
        private const int RowLookupLimit = 1000;

        public required List<ColumnRef> Columns { get; init; }
        public required List<ColumnRef> OtherColumns { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var table = Tables[0];
            var other = Tables[1];
            var result = NewResult(Name);

            var (missing, cleanupOnly) = ResultSql.MissingFrom(Columns, other, OtherColumns);
            var noMatch = AddCategory(context, result, table, missing, "No match", setDates: true);
            var cleanup = cleanupOnly != null
                ? AddCategory(context, result, table, cleanupOnly, "Match only after trimming spaces or ignoring case", setDates: false)
                : 0;

            result.Status = noMatch + cleanup > 0 ? CheckStatus.ProblemsFound : CheckStatus.NoProblemsFound;
            return [result];
        }

        /// <summary>
        /// One category of missing values: its counts, and up to ExampleLimit values, most rows first.
        /// Returns how many rows it holds.
        /// </summary>
        private long AddCategory(AnalysisContext context, CheckResult result, TableRef table, string where, string label, bool setDates)
        {
            var n = Columns.Count;
            var keys = ResultSql.List("t", Columns);
            var dates = ResultSql.DateRange(context, table, (aggregate, date) => $"{aggregate}({aggregate}(t.{date})) OVER ()");
            var rows = context.Database.QueryRows(
                $"SELECT TOP ({AnalysisContext.ExampleLimit}) {keys}, COUNT_BIG(*), COUNT_BIG(*) OVER (), SUM(COUNT_BIG(*)) OVER (), {dates}\n" +
                $"FROM {table.Name} AS t\nWHERE {where}\nGROUP BY {keys}\nORDER BY COUNT_BIG(*) DESC, {keys}");

            var values = rows.Count > 0 ? Convert.ToInt64(rows[0][n + 1], CultureInfo.InvariantCulture) : 0;
            var total = rows.Count > 0 ? Convert.ToInt64(rows[0][n + 2], CultureInfo.InvariantCulture) : 0;
            result.Counts.Add(new ResultCount($"{label}: values", values));
            result.Counts.Add(new ResultCount($"{label}: rows", total));
            if (rows.Count == 0)
            {
                return 0;
            }

            var range = ResultSql.RangeText(rows[0][n + 3], rows[0][n + 4]);
            if (setDates)
            {
                ResultSql.SetDates(result, rows[0][n + 3], rows[0][n + 4]);
            }
            // The counts are in the result's table above, so the heading adds only the dates.
            result.Details.Add($"{label}{(range != null ? $" (dated {range})" : "")}:");

            var shown = rows.Where(r => Columns.Select((c, i) => ResultSql.CanShow(c, r[i])).All(s => s)).ToList();
            var withheld = rows.Except(shown).ToList();
            result.Details.AddRange(shown.Select(row =>
                $"  {string.Join(", ", Columns.Select((c, i) => $"{c.Name} {ResultSql.ShowValue(c, row[i])}"))}: " +
                ResultSql.RowCount(Convert.ToInt64(row[n], CultureInfo.InvariantCulture))));
            if (withheld.Count > 0)
            {
                var hidden = withheld.SelectMany(r => Columns.Select((c, i) => (Column: c, Value: r[i])).Where(p => !ResultSql.CanShow(p.Column, p.Value))).ToList();
                hidden.ForEach(p => p.Column.NoteMasked(p.Value));
                var parts = hidden.Select(p => Convert.ToString(p.Value, CultureInfo.InvariantCulture)?.Trim() ?? "").ToList();
                result.Details.Add($"  {withheld.Count:N0} values not shown ({ResultSql.RowCount(withheld.Sum(r => Convert.ToInt64(r[n], CultureInfo.InvariantCulture)))}): {ResultSql.ShapeSummary(parts)}");
                AddWithheldRows(context, result, table, withheld);
            }
            if (values > rows.Count)
            {
                result.Details.Add($"  ... and {values - rows.Count:N0} more values, not listed.");
            }
            return total;
        }

        /// <summary>
        /// The rows holding values that are not shown, by key, so each can be looked up directly.
        /// </summary>
        private void AddWithheldRows(AnalysisContext context, CheckResult result, TableRef table, List<object?[]> withheld)
        {
            if (withheld.Count * Columns.Count > RowLookupLimit)
            {
                result.Details.Add($"  Too many values ({withheld.Count:N0}) to list their rows.");
                return;
            }

            var (match, parameters) = ResultSql.MatchAny(Columns.Select(c => $"t.{c.Sql}").ToList(), Columns, withheld);
            var rows = ResultSql.FindAffectedRows(context, table, match, parameters);
            if (table.KeyColumns.Count == 0)
            {
                result.Details.Add($"  ({table.Name} has no key to list their rows by.)");
                return;
            }
            var shown = rows.Keys.Count < rows.Total ? $"{rows.Keys.Count:N0} of {rows.Total:N0}" : $"{rows.Total:N0}";
            result.Details.Add($"  Rows by {ResultSql.KeyNames(table.KeyColumns)} ({shown}): {string.Join(", ", rows.Keys)}");
        }
    }
}
