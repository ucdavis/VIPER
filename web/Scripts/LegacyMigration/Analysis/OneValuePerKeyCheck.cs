using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class OneValuePerKeyDeclaration : CheckDeclaration
    {
        public required List<string> KeyColumns { get; init; }
        public required List<string> Columns { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var keys = binder.Columns(where, "keyColumns", table, KeyColumns);
            var columns = binder.Columns(where, "columns", table, Columns);
            if (table == null || keys == null || columns == null)
            {
                return null;
            }
            if (columns.Exists(keys.Contains))
            {
                binder.Error(where, "a column cannot be in both \"keyColumns\" and \"columns\".");
                return null;
            }

            return new OneValuePerKeyCheck
            {
                Name = $"oneValuePerKey: {string.Join(", ", columns.Select(c => c.Name))}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [table],
                KeyColumns = keys,
                Columns = columns
            };
        }
    }

    /// <summary>
    /// For each column, the keys whose rows hold more than one value of it. NULL next to a value counts as
    /// a difference. Values compare as SQL Server compares them (the column's collation, trailing spaces
    /// ignored). Rows with a NULL in any key column belong to no key and are skipped. Each key shown is
    /// broken down into its groups of rows that share a value, by row key and date, so the rows that
    /// disagree can be found without the values reaching the report.
    /// </summary>
    public sealed class OneValuePerKeyCheck : DeclaredCheck
    {
        // Each key takes several lines, so fewer are shown than other checks' examples.
        private const int KeyLimit = 10;

        // Row keys listed for each group of rows that share a value.
        private const int RowKeyLimit = 10;

        public required List<ColumnRef> KeyColumns { get; init; }
        public required List<ColumnRef> Columns { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context) =>
            Columns.Select(c => RunColumn(context, c)).ToList();

        private CheckResult RunColumn(AnalysisContext context, ColumnRef column)
        {
            var table = column.Table;
            var keys = ResultSql.List("t", KeyColumns);
            var values = $"COUNT_BIG(DISTINCT {column.ComparableSql("t")}) + MAX(CASE WHEN t.{column.Sql} IS NULL THEN 1 ELSE 0 END)";

            // The window runs over the keys that pass HAVING, so the range covers every row of every such key.
            var dates = ResultSql.DateRange(context, table, (aggregate, date) => $"{aggregate}({aggregate}(t.{date})) OVER ()");
            var rows = context.Database.QueryRows(
                $"SELECT TOP ({KeyLimit}) {keys}, {values}, COUNT_BIG(*) OVER (), {dates}\n" +
                $"FROM {table.Name} AS t\nWHERE {ResultSql.NotNull("t", KeyColumns)}\nGROUP BY {keys}\nHAVING {values} > 1\nORDER BY {keys}");

            var keyText = KeyColumns.Count == 1 ? KeyColumns[0].Name : $"({string.Join(", ", KeyColumns.Select(k => k.Name))})";
            var result = NewResult($"oneValuePerKey: {column.Name} per {keyText}");
            var total = rows.Count > 0 ? Convert.ToInt64(rows[0][KeyColumns.Count + 1], CultureInfo.InvariantCulture) : 0;
            result.Counts.Add(new ResultCount("Keys with more than one value", total));
            if (rows.Count > 0)
            {
                ResultSql.SetDates(result, rows[0][KeyColumns.Count + 2], rows[0][KeyColumns.Count + 3]);
                var shown = rows.Count < total ? $"{rows.Count:N0} of {total:N0}" : $"{total:N0}";
                result.Details.Add($"Keys with more than one {column.Name} value ({shown}):");
                AddBreakdown(context, column, rows, result);
                if (total > rows.Count)
                {
                    result.Details.Add($"... and {total - rows.Count:N0} more keys, not broken down.");
                }
            }
            result.Status = total > 0 ? CheckStatus.ProblemsFound : CheckStatus.NoProblemsFound;
            return result;
        }

        /// <summary>
        /// For each example key, its rows grouped by value: how many rows each group has, their row keys,
        /// and their dates. The largest group, when it is larger than every other, is only counted, since
        /// the rows that differ from it are the ones to look at. SQL numbers the keys and the groups (DENSE_RANK
        /// compares as the columns do), so keys differing only in case or trailing spaces stay one key, as in
        /// the count, and the values themselves are read only when the column's can be shown.
        /// </summary>
        private void AddBreakdown(AnalysisContext context, ColumnRef column, List<object?[]> exampleKeys, CheckResult result)
        {
            var readsValues = column.Visibility != ValueVisibility.Masked;
            var (matches, parameters) = ResultSql.MatchAny(KeyColumns.Select(k => $"t.{k.Sql}").ToList(), KeyColumns, exampleKeys, "k");
            var table = column.Table;
            var rowKeys = table.KeyColumns;
            var date = context.Plan.DateColumns.GetValueOrDefault(table);
            var keys = ResultSql.List("t", KeyColumns);
            var value = column.ComparableSql("t");
            var select = new[]
            {
                keys,
                $"DENSE_RANK() OVER (ORDER BY {keys})",
                $"DENSE_RANK() OVER (PARTITION BY {keys} ORDER BY {value})",
                $"CASE WHEN t.{column.Sql} IS NULL THEN 1 ELSE 0 END",
                readsValues ? value : "NULL",
                date != null ? $"t.{date.Sql}" : "NULL"
            }.Concat(rowKeys.Select(k => $"t.{k.Sql}"));
            var order = rowKeys.Count > 0 ? $"{keys}, {ResultSql.List("t", rowKeys)}" : keys;
            var rows = context.Database.QueryRows(
                $"SELECT {string.Join(", ", select)}\nFROM {table.Name} AS t\nWHERE {matches}\nORDER BY {order}", parameters);

            // After the key: the key's number, the value's number within it, the NULL flag, the value, the date, then the row key.
            var n = KeyColumns.Count;
            // Grouped by SQL's number for the key, not its text, which may describe two person keys by the same shape.
            foreach (var key in rows.GroupBy(r => Convert.ToInt64(r[n], CultureInfo.InvariantCulture)))
            {
                var keyText = ResultSql.NamedKeyText(KeyColumns, key.First(), 0);
                var groups = key
                    .GroupBy(r => Convert.ToInt64(r[n + 1], CultureInfo.InvariantCulture))
                    .Select(g => (
                        IsNull: Convert.ToInt32(g.First()[n + 2], CultureInfo.InvariantCulture) == 1,
                        Value: g.First()[n + 3],
                        Rows: g.Count(),
                        RowKeys: rowKeys.Count > 0 ? g.Select(r => ResultSql.KeyText(rowKeys, r, n + 5)).ToList() : [],
                        Range: ResultSql.RangeText(g.Min(r => ResultSql.AsDate(r[n + 4])), g.Max(r => ResultSql.AsDate(r[n + 4])))))
                    .OrderByDescending(g => g.Rows)
                    .ToList();
                var hasMostCommon = groups.Count > 1 && groups[0].Rows > groups[1].Rows;

                result.Details.Add($"  {keyText}: {ResultSql.RowCount(key.Count())}, {groups.Count} different {column.Name} values");
                for (var i = 0; i < groups.Count; i++)
                {
                    var g = groups[i];
                    var shows = readsValues && ResultSql.CanShow(column, g.Value);
                    if (!g.IsNull && !shows)
                    {
                        column.NoteMasked(g.Value);
                    }
                    var label = g.IsNull ? "NULL"
                        : shows ? ResultSql.ShowValue(column, g.Value)
                        : i == 0 && hasMostCommon ? "the most common value"
                        : hasMostCommon ? "a different value"
                        : i == 0 ? "one value" : "another value";
                    var dated = g.Range != null ? $" (dated {g.Range})" : "";
                    if (i == 0 && hasMostCommon)
                    {
                        result.Details.Add($"    {ResultSql.RowCount(g.Rows)}: {label}{dated}");
                        continue;
                    }
                    var listed = g.RowKeys.Count > 0 ? $", {ResultSql.KeyNames(rowKeys)} {string.Join(", ", g.RowKeys.Take(RowKeyLimit))}" : "";
                    var more = g.RowKeys.Count > RowKeyLimit ? $", and {g.RowKeys.Count - RowKeyLimit:N0} more" : "";
                    result.Details.Add($"    {ResultSql.RowCount(g.Rows)}: {label}{listed}{more}{dated}");
                }
            }
        }
    }
}
