using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed record JoinPair(string Column, string OtherColumn);

    public sealed class MatchesOtherTableDeclaration : CheckDeclaration
    {
        public required string Column { get; init; }
        public required string OtherTable { get; init; }
        // Entries are nullable because the reader lets a null list entry through (Declarations.ReadOptions).
        public required IReadOnlyList<JoinPair?> JoinOn { get; init; }
        public required string OtherColumn { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var other = binder.Object(where, "otherTable", OtherTable);
            var column = binder.Column(where, "column", table, Column);
            var otherColumn = binder.Column(where, "otherColumn", other, OtherColumn);
            if (JoinOn is not { Count: > 0 })
            {
                binder.Error(where, "\"joinOn\" needs at least one { \"column\": ..., \"otherColumn\": ... } pair.");
                return null;
            }
            var pairs = JoinOn.OfType<JoinPair>().ToList();
            if (pairs.Count < JoinOn.Count)
            {
                binder.Error(where, "\"joinOn\" has a null entry; write each as a { \"column\": ..., \"otherColumn\": ... } pair.");
                return null;
            }
            var joinColumns = binder.Columns(where, "joinOn column", table, pairs.Select(j => j.Column).ToList());
            var otherJoinColumns = binder.Columns(where, "joinOn otherColumn", other, pairs.Select(j => j.OtherColumn).ToList());
            if (table == null || other == null || column == null || otherColumn == null || joinColumns == null || otherJoinColumns == null)
            {
                return null;
            }
            if (Kind(column) == null || Kind(column) != Kind(otherColumn))
            {
                binder.Error(where, $"{column.Name} is {column.Summary.Type} and {otherColumn.Name} is {otherColumn.Summary.Type}; " +
                                    "matchesOtherTable compares two text, two number, or two date columns.");
                return null;
            }

            return new MatchesOtherTableCheck
            {
                Name = $"matchesOtherTable: {column.Name} vs {other.Name}.{otherColumn.Name}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [table, other],
                Column = column,
                OtherColumn = otherColumn,
                JoinColumns = joinColumns,
                OtherJoinColumns = otherJoinColumns
            };
        }

        private static string? Kind(ColumnRef column) =>
            column.IsText ? "text" : column.IsNumber ? "number" : column.IsDate ? "date" : null;
    }

    /// <summary>
    /// Whether a copied column still agrees with the column it was copied from. Each row of the table is
    /// joined to the other table's row whose joinOn columns are equal; two NULLs agree. A NULL on one side
    /// against a value on the other is counted on its own, by side, since it usually means a copy never
    /// filled in rather than one that drifted. Text compares exactly except for trailing spaces, which SQL
    /// Server ignores, and values that differ only in case or surrounding spaces are counted on their own. Rows with no
    /// matching row, or several, are counted separately, since there is nothing single to compare with.
    /// Rows with a NULL in a joinOn column are skipped. The differing value pairs are read only when both
    /// columns' values can be shown (ResultSql.CanShow).
    /// </summary>
    public sealed class MatchesOtherTableCheck : DeclaredCheck
    {
        private const int PairLimit = 10;

        public required ColumnRef Column { get; init; }
        public required ColumnRef OtherColumn { get; init; }
        public required List<ColumnRef> JoinColumns { get; init; }
        public required List<ColumnRef> OtherJoinColumns { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var table = Tables[0];
            var other = Tables[1];
            var result = NewResult(Name);

            var join = ResultSql.Equal("t", JoinColumns, "p", OtherJoinColumns);
            var joined = $"{ResultSql.NotNull("t", JoinColumns)}\n  AND (SELECT COUNT_BIG(*) FROM {other.Name} AS p WHERE {join})";
            var same = Same("t", "p", exact: true);
            var sameAfterCleanup = Same("t", "p", exact: false);

            var bothValues = $"t.{Column.Sql} IS NOT NULL AND p.{OtherColumn.Sql} IS NOT NULL";
            var differ = $"{joined} = 1\n  AND EXISTS (SELECT 1 FROM {other.Name} AS p WHERE {join} AND {bothValues} AND NOT ({same}) AND NOT ({sameAfterCleanup}))";
            var differCleanupOnly = Column.IsText
                ? $"{joined} = 1\n  AND EXISTS (SELECT 1 FROM {other.Name} AS p WHERE {join} AND {bothValues} AND NOT ({same}) AND ({sameAfterCleanup}))"
                : "1 = 0";
            var nullHere = $"{joined} = 1\n  AND t.{Column.Sql} IS NULL AND EXISTS (SELECT 1 FROM {other.Name} AS p WHERE {join} AND p.{OtherColumn.Sql} IS NOT NULL)";
            var nullThere = $"{joined} = 1\n  AND t.{Column.Sql} IS NOT NULL AND EXISTS (SELECT 1 FROM {other.Name} AS p WHERE {join} AND p.{OtherColumn.Sql} IS NULL)";

            var rows = ResultSql.AddAffectedRows(context, result, table, differ);
            result.Counts.Add(new ResultCount("Rows that disagree", rows));
            var cleanupOnly = AddCategory(context, result, table, differCleanupOnly, "Differ only in case or surrounding spaces", Column.IsText);
            var missingHere = AddCategory(context, result, table, nullHere, $"NULL in {table.Name}, value in {other.Name}", true);
            var missingThere = AddCategory(context, result, table, nullThere, $"Value in {table.Name}, NULL in {other.Name}", true);
            var none = AddCategory(context, result, table, $"{joined} = 0", $"No matching row in {other.Name}", true);
            var several = AddCategory(context, result, table, $"{joined} > 1", $"Several matching rows in {other.Name}", true);

            if (rows > 0)
            {
                var masked = new[] { Column, OtherColumn }.Where(c => c.Visibility == ValueVisibility.Masked).ToList();
                if (masked.Count == 0)
                {
                    AddPairs(context, result, table, other, join, differ);
                }
                masked.ForEach(c => c.NoteMasked());
            }
            result.Status = rows + cleanupOnly + missingHere + missingThere + none + several > 0 ? CheckStatus.ProblemsFound : CheckStatus.NoProblemsFound;
            return [result];
        }

        /// <summary>
        /// Two NULLs agree. Exact text compares under a binary collation; after cleanup, trimmed and
        /// ignoring case. Wrapped in CASE so a NULL against a value is false rather than unknown, which
        /// NOT would leave unknown and drop the row.
        /// </summary>
        private string Same(string alias, string otherAlias, bool exact)
        {
            var nulls = $"({alias}.{Column.Sql} IS NULL AND {otherAlias}.{OtherColumn.Sql} IS NULL)";
            var value = Column.ComparableSql(alias);
            var otherValue = OtherColumn.ComparableSql(otherAlias);
            var equal = !Column.IsText ? $"{value} = {otherValue}"
                : exact ? $"{value} COLLATE Latin1_General_BIN2 = {otherValue} COLLATE Latin1_General_BIN2"
                : $"LTRIM(RTRIM({value})) COLLATE Latin1_General_CI_AS = LTRIM(RTRIM({otherValue})) COLLATE Latin1_General_CI_AS";
            return $"(CASE WHEN {equal} OR {nulls} THEN 1 ELSE 0 END) = 1";
        }

        /// <summary>
        /// One further category as a count and a detail line of row keys with their date range.
        /// </summary>
        private static long AddCategory(AnalysisContext context, CheckResult result, TableRef table, string where, string label, bool applies)
        {
            if (!applies)
            {
                return 0;
            }
            var found = ResultSql.FindAffectedRows(context, table, where);
            result.Counts.Add(new ResultCount(label, found.Total));
            if (found.Total > 0)
            {
                var dated = ResultSql.RangeText(found.Earliest, found.Latest) is { } range ? $"; dated {range}" : "";
                var keys = found.Keys.Count > 0
                    ? $" by {ResultSql.KeyNames(table.KeyColumns)} ({(found.Keys.Count < found.Total ? $"{found.Keys.Count:N0} of {found.Total:N0}" : $"{found.Total:N0}")}): {string.Join(", ", found.Keys)}"
                    : $" ({found.Total:N0})";
                result.Details.Add($"{label}{keys}{dated}");
            }
            return found.Total;
        }

        /// <summary>
        /// The differing value pairs shared by the most rows.
        /// </summary>
        private void AddPairs(AnalysisContext context, CheckResult result, TableRef table, TableRef other, string join, string differ)
        {
            var value = Column.ComparableSql("t");
            var otherValue = OtherColumn.ComparableSql("p");
            var pairs = context.Database.QueryRows(
                $"SELECT TOP ({PairLimit}) {value}, {otherValue}, COUNT_BIG(*)\nFROM {table.Name} AS t\nJOIN {other.Name} AS p ON {join}\n" +
                $"WHERE {differ}\nGROUP BY {value}, {otherValue}\nORDER BY COUNT_BIG(*) DESC");
            result.Details.Add($"Differing values, {Column.Name} vs {other.Name}.{OtherColumn.Name}:");
            result.Details.AddRange(pairs.Select(pair =>
                $"  {ResultSql.ShowValue(Column, pair[0])} vs {ResultSql.ShowValue(OtherColumn, pair[1])}: {ResultSql.RowCount(Convert.ToInt64(pair[2], CultureInfo.InvariantCulture))}"));
        }
    }
}
