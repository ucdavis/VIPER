using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class NoDuplicatesDeclaration : CheckDeclaration
    {
        public required List<string> Columns { get; init; }
        public bool IgnoreCase { get; init; }
        public bool IgnoreSurroundingSpaces { get; init; }
        public bool IgnoreLeadingZeros { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var columns = binder.Columns(where, "columns", table, Columns);
            if (table == null || columns == null)
            {
                return null;
            }

            return new NoDuplicatesCheck
            {
                Name = $"noDuplicates: {ResultSql.KeyNames(columns)}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [table],
                Columns = columns,
                IgnoreCase = IgnoreCase,
                IgnoreSurroundingSpaces = IgnoreSurroundingSpaces,
                IgnoreLeadingZeros = IgnoreLeadingZeros
            };
        }
    }

    /// <summary>
    /// Combinations of values in the columns that more than one row shares. Text compares exactly
    /// (case, accents, and trailing spaces all count) unless the declaration says to ignore case,
    /// surrounding spaces, or leading zeros. Rows with a NULL in any of the columns are skipped; notBlank
    /// covers those. Reported as part of the table's Duplicate rows result (DuplicateRowsCheck), so rows
    /// that are exact copies are not reported twice: the groups whose rows differ elsewhere are listed
    /// with the columns that differ, and those that are exact copies only counted.
    /// </summary>
    public sealed class NoDuplicatesCheck : DeclaredCheck
    {
        private const int GroupLimit = 10;
        private const int RowKeyLimit = 10;

        public required List<ColumnRef> Columns { get; init; }
        public bool IgnoreCase { get; init; }
        public bool IgnoreSurroundingSpaces { get; init; }
        public bool IgnoreLeadingZeros { get; init; }

        /// <summary>
        /// Nothing of its own: DuplicateRowsCheck runs every declaration through AddSection.
        /// </summary>
        public override IReadOnlyList<CheckResult> Run(AnalysisContext context) => [];

        /// <summary>
        /// Adds this key's part to a Duplicate rows result: groups sharing the key whose rows differ in
        /// some compared column, listed with the columns that differ (never their values), then how many
        /// groups are exact copies. When exactCopiesListed, the result already lists those, so they are
        /// only counted; otherwise they are listed here too. Returns whether any group was found.
        /// </summary>
        public bool AddSection(AnalysisContext context, TableRef table, List<ColumnRef> compared, bool exactCopiesListed,
            List<ResultCount> counts, List<string> details)
        {
            // Each column's compared value, then the hidden expressions that make the comparison exact.
            var expressions = Columns.Select(Compared).Concat(Columns.Select(Exactness).OfType<string>()).ToList();
            var group = string.Join(", ", expressions);
            var notNull = ResultSql.NotNull("t", Columns);
            var dates = ResultSql.DateRange(context, table, (aggregate, date) => $"{aggregate}(t.{date})");
            // 1 where the group's rows hold more than one value in the column (NULL counts as a value); 2
            // where they hold one only under the column's collation, differing in case or trailing spaces.
            var differs = compared.Select(c =>
            {
                var more = $"COUNT(DISTINCT {DuplicateRowsCheck.Comparable(c)}) + MAX(CASE WHEN t.{c.Sql} IS NULL THEN 1 ELSE 0 END) > 1";
                return c.IsText
                    ? $"CASE WHEN {more} THEN 1 WHEN {DuplicateRowsCheck.DiffersOnlyLoosely(c)} THEN 2 ELSE 0 END"
                    : $"CASE WHEN {more} THEN 1 ELSE 0 END";
            });
            var groups = context.Database.QueryRows(
                $"SELECT {string.Join(", ", expressions.Append("COUNT_BIG(*)").Append(dates).Concat(differs))}\n" +
                $"FROM {table.Name} AS t\nWHERE {notNull}\nGROUP BY {group}\nHAVING COUNT_BIG(*) > 1");

            var n = expressions.Count;
            var found = groups.Select(g =>
                {
                    var flags = compared.Select((c, i) => (Column: c, Flag: Convert.ToInt32(g[n + 3 + i], CultureInfo.InvariantCulture))).ToList();
                    return (
                        Row: g,
                        Rows: Convert.ToInt64(g[n], CultureInfo.InvariantCulture),
                        Strict: flags.Where(f => f.Flag == 1).Select(f => f.Column).ToList(),
                        Loose: flags.Where(f => f.Flag == 2).Select(f => f.Column).ToList(),
                        // In the table's column order, as the report names them.
                        Differing: flags.Where(f => f.Flag > 0)
                            .Select(f => f.Flag == 1 ? f.Column.Name : $"{f.Column.Name} ({DuplicateRowsCheck.LooseDifference} only)").ToList());
                })
                .ToList();
            var differing = found.Where(f => f.Strict.Count > 0).ToList();
            // Copies: no column holds two values, though some may differ in case or trailing spaces.
            var copies = found.Where(f => f.Strict.Count == 0).ToList();
            var label = $"Same {ResultSql.KeyNames(Columns)}";

            // Copies the result already lists are only mentioned, so the same groups are not counted
            // twice: "46 of the 57 groups sharing it; the other 11 are exact copies, above".
            var copyKind = copies.TrueForAll(f => f.Loose.Count == 0)
                ? (copies.Count == 1 ? "is an exact copy" : "are exact copies")
                : (copies.Count == 1 ? "is a copy" : $"are copies, exact or differing only in {DuplicateRowsCheck.LooseDifference}");
            var exactNote = exactCopiesListed && copies.Count > 0 ? $"the other {copies.Count:N0} {copyKind}, above" : null;

            if (differing.Count > 0)
            {
                counts.Add(new ResultCount($"{label}, different elsewhere: groups", differing.Count));
                counts.Add(new ResultCount($"{label}, different elsewhere: rows", differing.Sum(f => f.Rows)));
                var notes = new[]
                {
                    exactNote != null ? $"{differing.Count:N0} of the {found.Count:N0} groups sharing it; {exactNote}" : null,
                    Dated(differing.Select(f => f.Row), n)
                }.OfType<string>().ToList();
                details.Add($"{label}, different elsewhere{(notes.Count > 0 ? $" ({string.Join("; ", notes)})" : "")}:");
                if (Description != null)
                {
                    details.Add($"  {Description}");
                }
                AddDifferingCombinations(differing.Select(f => f.Differing).ToList(), details);
                AddGroups(context, table, expressions, differing.OrderByDescending(f => f.Rows).Select(f => (f.Row, f.Rows, f.Differing)).ToList(), details);
            }
            else if (exactNote != null)
            {
                details.Add($"{label}: {(copies.Count == 1 ? "the one group sharing it" : $"all {copies.Count:N0} groups sharing it")} {copyKind}, above.");
            }
            if (!exactCopiesListed)
            {
                foreach (var (kind, kindCopies) in new[]
                {
                    ("exact copies", copies.Where(f => f.Loose.Count == 0).ToList()),
                    ($"copies differing only in {DuplicateRowsCheck.LooseDifference}", copies.Where(f => f.Loose.Count > 0).ToList())
                }.Where(k => k.Item2.Count > 0))
                {
                    counts.Add(new ResultCount($"{label}, {kind}: groups", kindCopies.Count));
                    counts.Add(new ResultCount($"{label}, {kind}: rows", kindCopies.Sum(f => f.Rows)));
                    details.Add($"{label}, {kind}{(Dated(kindCopies.Select(f => f.Row), n) is { } dated ? $" ({dated})" : "")}:");
                    AddGroups(context, table, expressions, kindCopies.OrderByDescending(f => f.Rows).Select(f => (f.Row, f.Rows, f.Differing)).ToList(), details);
                }
            }
            if (found.Count == 0)
            {
                details.Add($"{label}: no rows share it.");
            }
            return found.Count > 0;
        }

        // Combinations of differing columns listed before the rest are summed up.
        private const int CombinationLimit = 10;

        /// <summary>
        /// How many groups differ in each combination of columns, such as "Modified_date only: 32", so
        /// groups that differ only in a timestamp (the same record saved twice) stand apart from real
        /// conflicts.
        /// </summary>
        private static void AddDifferingCombinations(List<List<string>> differing, List<string> details)
        {
            var combinations = differing
                .GroupBy(d => d.Count == 1 ? $"{d[0]} only" : ResultSql.JoinAnd(d), StringComparer.Ordinal)
                .Select(g => (Label: g.Key, Groups: g.Count()))
                .OrderByDescending(c => c.Groups)
                .ToList();
            details.Add("  Columns that differ (groups):");
            details.AddRange(combinations.Take(CombinationLimit).Select(c => $"    {c.Label}: {c.Groups:N0}"));
            if (combinations.Count > CombinationLimit)
            {
                details.Add($"    {combinations.Count - CombinationLimit:N0} other combinations: {combinations.Skip(CombinationLimit).Sum(c => c.Groups):N0}");
            }
        }

        private static string? Dated(IEnumerable<object?[]> rows, int n)
        {
            var list = rows.ToList();
            var earliest = list.Select(r => ResultSql.AsDate(r[n + 1])).Where(d => d != null).Min();
            var latest = list.Select(r => ResultSql.AsDate(r[n + 2])).Where(d => d != null).Max();
            return ResultSql.RangeText(earliest, latest) is { } range ? $"dated {range}" : null;
        }

        /// <summary>
        /// The groups sharing the most rows, one per line, each with its rows by key and, where they
        /// differ, the columns that do.
        /// </summary>
        private void AddGroups(AnalysisContext context, TableRef table, List<string> expressions,
            List<(object?[] Row, long Rows, List<string> Differing)> groups, List<string> details)
        {
            var listed = groups.Take(GroupLimit).ToList();
            details.Add($"  Groups ({(listed.Count < groups.Count ? $"{listed.Count:N0} of {groups.Count:N0}" : $"{groups.Count:N0}")}):");
            var rowKeys = RowKeysByGroup(context, table, expressions, listed.Select(g => g.Row).ToList());
            for (var i = 0; i < listed.Count; i++)
            {
                var (row, rows, differing) = listed[i];
                var value = string.Join(", ", Columns.Select((c, j) => $"{c.Name} {ResultSql.ShowValue(c, row[j])}"));
                var keys = rowKeys.GetValueOrDefault(i, []);
                var keyList = keys.Count > 0
                    ? $"; {ResultSql.KeyNames(table.KeyColumns)} {string.Join(", ", keys.Take(RowKeyLimit))}{(keys.Count > RowKeyLimit ? $", and {keys.Count - RowKeyLimit:N0} more" : "")}"
                    : "";
                var differ = differing.Count > 0 ? $"; differ in {ResultSql.JoinAnd(differing)}" : "";
                details.Add($"    {value}: {ResultSql.RowCount(rows)}{keyList}{differ}");
            }
        }

        /// <summary>
        /// Each listed group's row keys, found by numbering the groups in SQL, so rows match their group
        /// by the same comparison that grouped them.
        /// </summary>
        private Dictionary<int, List<string>> RowKeysByGroup(AnalysisContext context, TableRef table, List<string> expressions,
            List<object?[]> groups)
        {
            if (table.KeyColumns.Count == 0 || groups.Count == 0)
            {
                return [];
            }

            var parameters = new List<SqlParameter>();
            var cases = groups.Select((row, i) =>
            {
                var (match, matchParameters) = ResultSql.MatchAny(expressions, Columns, [row.Take(expressions.Count).ToArray()], $"g{i}_");
                parameters.AddRange(matchParameters);
                return $"WHEN {match} THEN {i}";
            });
            var rows = context.Database.QueryRows(
                $"SELECT x.g, {ResultSql.List("x", table.KeyColumns)}\n" +
                $"FROM (SELECT CASE {string.Join(" ", cases)} END AS g, {ResultSql.List("t", table.KeyColumns)} FROM {table.Name} AS t) AS x\n" +
                $"WHERE x.g IS NOT NULL\nORDER BY x.g, {ResultSql.List("x", table.KeyColumns)}", parameters.ToArray());

            return rows
                .GroupBy(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture))
                .ToDictionary(g => g.Key, g => g.Select(r => ResultSql.KeyText(table.KeyColumns, r, 1)).ToList());
        }

        /// <summary>
        /// The value a column is compared by: text trimmed, stripped of leading zeros, and collated as
        /// the declaration asks; other types as they are.
        /// </summary>
        private string Compared(ColumnRef column)
        {
            if (!column.IsText)
            {
                return $"t.{column.Sql}";
            }
            var value = Normalized(column);
            return $"{value} COLLATE {(IgnoreCase ? "Latin1_General_CI_AS" : "Latin1_General_BIN2")}";
        }

        /// <summary>
        /// For text compared with its surrounding spaces, its length in bytes, since SQL Server ignores
        /// trailing spaces when comparing.
        /// </summary>
        private string? Exactness(ColumnRef column) =>
            column.IsText && !IgnoreSurroundingSpaces ? $"DATALENGTH({Normalized(column)})" : null;

        private string Normalized(ColumnRef column)
        {
            var value = column.ComparableSql("t");
            if (IgnoreSurroundingSpaces)
            {
                value = $"LTRIM(RTRIM({value}))";
            }
            if (IgnoreLeadingZeros)
            {
                // Blanks stay blank; all zeros become a single 0.
                value = $"(CASE WHEN {value} = '' THEN {value} WHEN {value} NOT LIKE '%[^0]%' THEN '0' " +
                        $"ELSE SUBSTRING({value}, PATINDEX('%[^0]%', {value}), LEN({value})) END)";
            }
            return value;
        }
    }
}
