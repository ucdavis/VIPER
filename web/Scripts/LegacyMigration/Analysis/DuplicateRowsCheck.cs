using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// Duplicate rows. Without ignoringSurrogateKey: groups of fully identical rows in tables with no
    /// primary key, identity column, or enforced unique key over every row (a disabled or filtered one
    /// does not count), where nothing stops whole rows being stored twice;
    /// those rows have no key, so only counts are reported. With it: one result per table holding every
    /// kind of duplicate, so the same rows are not reported twice. Exact copies are rows identical in
    /// every column but an identity or single number primary key, which usually means the same record
    /// was entered twice. Each noDuplicates declaration on the table adds the rows that share its key but
    /// differ elsewhere, naming the columns that differ (conflicting versions of one record); its groups
    /// that are exact copies are counted, not listed again. Computed columns and types that cannot be
    /// compared are left out of every comparison, and the result says which.
    /// </summary>
    public sealed class DuplicateRowsCheck(bool ignoringSurrogateKey) : IAutomaticCheck
    {
        // Groups listed in the report, and rows read to fill them.
        private const int GroupLimit = 10;

        // Not comparable, or (rowversion) different on every row by design.
        private static readonly HashSet<string> SkippedTypes = new(StringComparer.Ordinal)
        {
            "xml", "geography", "geometry", "timestamp", "rowversion"
        };

        public string Name => ignoringSurrogateKey ? "Duplicate rows" : "Duplicate rows in tables with no key";

        private string Description => ignoringSurrogateKey
            ? "Rows that repeat a record. Exact copies are identical in every column but their identity or primary key, " +
              "which usually means the same record was entered twice; copies that match only under the columns' collation, " +
              $"differing in {LooseDifference}, are counted apart. Each declared key (noDuplicates) adds the rows that " +
              "share it but differ elsewhere, with the columns that differ: conflicting versions of one record. Rows counts " +
              "every row in the groups."
            : "Fully identical rows in a table with no primary key, identity column, or enforced unique key over every row " +
              "(a disabled or filtered one does not count), with rows that match only under " +
              $"the columns' collation, differing in {LooseDifference}, counted apart.";

        private Severity DefaultSeverity => ignoringSurrogateKey ? Severity.Warning : Severity.Critical;

        public IEnumerable<CheckResult> Run(AnalysisContext context)
        {
            var declared = context.Plan.DeclaredChecks.OfType<NoDuplicatesCheck>()
                .Where(_ => ignoringSurrogateKey)
                .ToLookup(c => c.Tables[0].Name, StringComparer.Ordinal);
            // A table outside "include" comes in only for its declarations, without the exact-copy part.
            var tables = context.Plan.Schema.Tables.Concat(context.Plan.Schema.Views)
                .Where(t => !t.IsExcluded)
                .Select(t => (Table: t, Key: t.IsView ? null : SurrogateKey(t), Declared: declared[t.Name].ToList()))
                .Where(t => ignoringSurrogateKey
                    ? (t.Key != null && t.Table.IsIncluded) || t.Declared.Count > 0
                    : t.Table.IsIncluded && !t.Table.IsView && t.Key == null && !HasKey(t.Table))
                .OrderBy(t => t.Table.Name, StringComparer.Ordinal)
                .ToList();

            var results = new List<CheckResult>();
            var passedDeclared = new List<CheckResult>();
            foreach (var (table, key, declarations) in tables)
            {
                Console.WriteLine($"  {table.Name}");
                var result = ignoringSurrogateKey ? RunCombined(context, table, key, declarations) : RunKeyless(context, table);
                results.Add(result);
                // A declared check that passed is still reported, as every declared check is.
                if (declarations.Count > 0 && result.Status == CheckStatus.NoProblemsFound)
                {
                    passedDeclared.Add(result);
                }
            }
            return AutomaticResult.OnlyFindings(results, Name, Description, DefaultSeverity,
                $"{tables.Count.ToString("N0", CultureInfo.InvariantCulture)} tables; none found").Concat(passedDeclared);
        }

        private CheckResult RunKeyless(AnalysisContext context, TableRef table)
        {
            var result = AutomaticResult.For(context, Name, Description, ResultKind.Problem, DefaultSeverity, table.Name);
            if (result.Status == CheckStatus.Skipped)
            {
                return result;
            }
            try
            {
                var compared = Compared(table, null);
                if (compared.Count == 0)
                {
                    result.Details.Add("No columns to compare.");
                    return result;
                }
                var exact = ExactCopies(context, table, compared);
                if (exact.Groups > 0)
                {
                    result.Status = CheckStatus.ProblemsFound;
                    if (exact.ExactGroups > 0)
                    {
                        result.Counts.Add(new ResultCount("Groups of identical rows", exact.ExactGroups));
                        result.Counts.Add(new ResultCount("Extra copies (rows beyond the first in each group)", exact.ExactRows - exact.ExactGroups));
                    }
                    if (exact.LooseGroups > 0)
                    {
                        result.Counts.Add(new ResultCount($"Groups identical except for {LooseDifference}", exact.LooseGroups));
                        result.Counts.Add(new ResultCount("Extra copies in those groups", exact.LooseRows - exact.LooseGroups));
                    }
                    ResultSql.SetDates(result, exact.Earliest, exact.Latest);
                }
                result.Details.AddRange(ComparedWithout(table, null, compared));
            }
            catch (SqlException ex)
            {
                result.Status = CheckStatus.FailedToRun;
                result.Message = $"Failed to run: {context.ErrorText(ex)}";
            }
            return result;
        }

        /// <summary>
        /// One result for the table: exact copies (unless the table has no surrogate key, is outside
        /// "include", or the exact-copy part is skipped by an override), then each declared key. The result takes the most severe
        /// severity of the parts that found something.
        /// </summary>
        private CheckResult RunCombined(AnalysisContext context, TableRef table, ColumnRef? key, List<NoDuplicatesCheck> declarations)
        {
            var automaticOverride = context.Plan.FindOverride(Name, table.Name);
            var counts = new List<ResultCount>();
            var details = new List<string>();
            var severities = new List<Severity>();
            string? message = null;
            CheckStatus status;
            try
            {
                var compared = Compared(table, key);
                var exactPart = key != null && table.IsIncluded;
                var exactListed = exactPart && automaticOverride?.Skip != true && compared.Count > 0;
                if (exactPart && automaticOverride?.Skip == true)
                {
                    message = $"Exact copies skipped: {automaticOverride.Reason}";
                }
                else if (exactListed)
                {
                    var exact = ExactCopies(context, table, compared);
                    if (exact.Groups > 0)
                    {
                        severities.Add(automaticOverride?.Severity ?? DefaultSeverity);
                        counts.AddRange(CopyCounts(exact));
                        var shown = declarations.SelectMany(d => d.Columns).Distinct().ToList();
                        details.AddRange(ExactCopyGroups(context, table, key!, compared, exact, shown));
                    }
                    if (automaticOverride != null)
                    {
                        message = $"Exact copies severity changed: {automaticOverride.Reason}";
                    }
                }

                // Every declaration adds its part, so the parts are added first and filtered after.
                var parts = declarations
                    .Select(d => (d.Severity, Found: d.AddSection(context, table, compared, exactListed, counts, details)))
                    .ToList();
                severities.AddRange(parts.Where(p => p.Found).Select(p => p.Severity));
                details.AddRange(ComparedWithout(table, key, compared));
                status = severities.Count > 0 ? CheckStatus.ProblemsFound : CheckStatus.NoProblemsFound;
            }
            catch (SqlException ex)
            {
                status = CheckStatus.FailedToRun;
                message = $"Failed to run: {context.ErrorText(ex)}";
            }

            var result = new CheckResult
            {
                Name = Name,
                Description = Description,
                Kind = ResultKind.Problem,
                Severity = severities.Count > 0 ? severities.Min() : DefaultSeverity,
                Table = table.Name,
                // With nothing declared, a skipped exact-copy part is the whole check.
                Status = automaticOverride?.Skip == true && declarations.Count == 0 ? CheckStatus.Skipped : status,
                Message = automaticOverride?.Skip == true && declarations.Count == 0 ? $"Skipped: {automaticOverride.Reason}" : message
            };
            result.Counts.AddRange(counts);
            result.Details.AddRange(details);
            return result;
        }

        private static bool HasKey(TableRef table) =>
            (table.TableSummary is { } summary && (summary.HasPrimaryKey || summary.HasUniqueKey))
            || table.Columns.Any(c => c.Summary.Identity != null);

        /// <summary>
        /// The identity column, else a single-column primary key of a whole-number type: keys that say
        /// nothing about the record, so two rows differing only in them are the same record.
        /// </summary>
        internal static ColumnRef? SurrogateKey(TableRef table)
        {
            var identity = table.Columns.FirstOrDefault(c => c.Summary.Identity != null);
            if (identity != null)
            {
                return identity;
            }
            var primaryKey = table.TableSummary?.PrimaryKeyColumns ?? [];
            var column = primaryKey.Count == 1 ? table.FindColumn(primaryKey[0]) : null;
            return column?.BaseType is "int" or "bigint" or "smallint" or "tinyint" ? column : null;
        }

        /// <summary>
        /// The columns two rows must agree on to be copies: all but the surrogate key, computed columns,
        /// and types that cannot be compared.
        /// </summary>
        internal static List<ColumnRef> Compared(TableRef table, ColumnRef? key) =>
            table.Columns.Where(c => c != key && !c.Summary.IsComputed && !SkippedTypes.Contains(c.BaseType)).ToList();

        private static IEnumerable<string> ComparedWithout(TableRef table, ColumnRef? key, List<ColumnRef> compared)
        {
            var left = table.Columns.Where(c => c != key && !compared.Contains(c)).ToList();
            if (left.Count > 0)
            {
                yield return $"Compared without: {string.Join(", ", left.Select(c => $"{c.Name} ({(c.Summary.IsComputed ? "computed" : c.BaseType)})"))}";
            }
        }

        /// <summary>
        /// Groups and Rows count every group of copies; LooseGroups and LooseRows those among them that are
        /// not byte for byte identical (DiffersOnlyLoosely).
        /// </summary>
        private sealed record Copies(long Groups, long Rows, long LooseGroups, long LooseRows, object? Earliest, object? Latest)
        {
            public long ExactGroups => Groups - LooseGroups;
            public long ExactRows => Rows - LooseRows;
        }

        /// <summary>
        /// The exact-copy part's counts: exact copies, then copies differing only loosely, each when there are any.
        /// </summary>
        private static IEnumerable<ResultCount> CopyCounts(Copies copies)
        {
            if (copies.ExactGroups > 0)
            {
                yield return new ResultCount("Exact copies: groups", copies.ExactGroups);
                yield return new ResultCount("Exact copies: rows", copies.ExactRows);
            }
            if (copies.LooseGroups > 0)
            {
                yield return new ResultCount($"Copies differing only in {LooseDifference}: groups", copies.LooseGroups);
                yield return new ResultCount($"Copies differing only in {LooseDifference}: rows", copies.LooseRows);
            }
        }

        private static Copies ExactCopies(AnalysisContext context, TableRef table, List<ColumnRef> compared)
        {
            var group = string.Join(", ", compared.Select(Comparable));
            var dates = ResultSql.DateRange(context, table, (aggregate, date) => $"{aggregate}(t.{date})");
            var row = context.Database.QueryRows(
                $"SELECT COUNT_BIG(*), SUM(x.n), MIN(x.Earliest), MAX(x.Latest), SUM(x.loose), SUM(x.loose * x.n)\n" +
                $"FROM (SELECT COUNT_BIG(*) AS n, {AnyDiffersOnlyLoosely(compared)} AS loose, {dates} FROM {table.Name} AS t\n" +
                $"      GROUP BY {group}\n      HAVING COUNT_BIG(*) > 1) AS x").Single();
            var groups = Convert.ToInt64(row[0], CultureInfo.InvariantCulture);
            return groups == 0
                ? new Copies(0, 0, 0, 0, null, null)
                : new Copies(groups, Convert.ToInt64(row[1], CultureInfo.InvariantCulture), Convert.ToInt64(row[4], CultureInfo.InvariantCulture),
                    Convert.ToInt64(row[5], CultureInfo.InvariantCulture), row[2], row[3]);
        }

        /// <summary>
        /// The first groups, each as the keys of its rows, then the values of valueColumns (the declared
        /// keys' columns, which declarations may show), which every row in a group shares. Rows
        /// are read in key order, grouped by the lowest key in each group, until GroupLimit groups are filled.
        /// </summary>
        private static IEnumerable<string> ExactCopyGroups(AnalysisContext context, TableRef table, ColumnRef key, List<ColumnRef> compared,
            Copies exact, List<ColumnRef> valueColumns)
        {
            var group = string.Join(", ", compared.Select(Comparable));
            var shownSelect = string.Concat(valueColumns.Select((c, i) => $", t.{c.Sql} AS c{i}"));
            var shownOuter = string.Concat(valueColumns.Select((_, i) => $", x.c{i}"));
            var rows = context.Database.QueryRows(
                $"SELECT x.g, x.k{shownOuter}\nFROM (SELECT t.{key.Sql} AS k{shownSelect}, MIN(t.{key.Sql}) OVER (PARTITION BY {group}) AS g,\n" +
                $"             COUNT_BIG(*) OVER (PARTITION BY {group}) AS n\n      FROM {table.Name} AS t) AS x\n" +
                $"WHERE x.n > 1 AND x.g IN (SELECT TOP ({GroupLimit}) y.g FROM (SELECT MIN(t.{key.Sql}) AS g FROM {table.Name} AS t\n" +
                $"      GROUP BY {group} HAVING COUNT_BIG(*) > 1) AS y ORDER BY y.g)\nORDER BY x.g, x.k");

            var groups = rows.GroupBy(r => ResultSql.CellText(r[0])).ToList();
            var loose = exact.LooseGroups > 0
                ? LooseColumnsByGroup(context, table, key, compared, group, "COUNT_BIG(*) > 1", GroupLimit)
                : [];
            var shown = groups.Count < exact.Groups ? $"{groups.Count:N0} of {exact.Groups:N0}" : $"{exact.Groups:N0}";
            var dated = ResultSql.RangeText(exact.Earliest, exact.Latest) is { } range ? $"; dated {range}" : "";
            yield return exact.LooseGroups > 0
                ? $"Copies, by {key.Name} ({shown}{dated}; exact unless marked):"
                : $"Exact copies, by {key.Name} ({shown}{dated}):";
            foreach (var g in groups)
            {
                var values = valueColumns.Count > 0
                    ? $" ({string.Join(", ", valueColumns.Select((c, i) => $"{c.Name} {ResultSql.ShowValue(c, g.First()[2 + i])}"))})"
                    : "";
                yield return $"  {string.Join(", ", g.Select(r => ResultSql.KeyCellText(key, r[1])))}{values}{LooseNote(loose.GetValueOrDefault(g.Key))}";
            }
        }

        /// <summary>
        /// A column's value as copies are compared: as stored, under its own collation, with image read as
        /// varbinary(max), which can be grouped. A case-insensitive collation groups "Smith" with "smith ",
        /// so the same record entered twice is found; DiffersOnlyLoosely then tells such copies from exact ones.
        /// </summary>
        internal static string Comparable(ColumnRef column) =>
            column.BaseType == "image" ? $"CAST(t.{column.Sql} AS varbinary(max))" : column.ComparableSql("t");

        // How a copy that is not byte for byte identical is described in the report.
        internal const string LooseDifference = "case or trailing spaces";

        /// <summary>
        /// SQL, inside a GROUP BY, true when a text column's values in the group are not byte for byte the
        /// same: equal under the column's collation, they differ in case (or accents, under an
        /// accent-insensitive collation) or trailing spaces. A binary collation still ignores trailing
        /// spaces, so lengths are compared too.
        /// </summary>
        internal static string DiffersOnlyLoosely(ColumnRef column)
        {
            var e = column.ComparableSql("t");
            return $"(MIN({e} COLLATE Latin1_General_BIN2) <> MAX({e} COLLATE Latin1_General_BIN2) OR MIN(DATALENGTH({e})) <> MAX(DATALENGTH({e})))";
        }

        /// <summary>
        /// SQL, inside a GROUP BY, 1 when any text column differs only loosely in the group, else 0.
        /// </summary>
        internal static string AnyDiffersOnlyLoosely(List<ColumnRef> columns)
        {
            var text = columns.Where(c => c.IsText).ToList();
            return text.Count > 0 ? $"CASE WHEN {string.Join(" OR ", text.Select(DiffersOnlyLoosely))} THEN 1 ELSE 0 END" : "0";
        }

        /// <summary>
        /// For the first GroupLimit groups by lowest key, the same groups the listings read, the text columns
        /// whose values differ only loosely, by the group's lowest key as CellText writes it. Empty when no
        /// group does.
        /// </summary>
        internal static Dictionary<string, List<string>> LooseColumnsByGroup(AnalysisContext context, TableRef table, ColumnRef key,
            List<ColumnRef> columns, string group, string having, int groupLimit)
        {
            var text = columns.Where(c => c.IsText).ToList();
            if (text.Count == 0)
            {
                return [];
            }
            var flags = text.Select(c => $"CASE WHEN {DiffersOnlyLoosely(c)} THEN 1 ELSE 0 END");
            var rows = context.Database.QueryRows(
                $"SELECT TOP ({groupLimit}) MIN(t.{key.Sql}), {string.Join(", ", flags)}\nFROM {table.Name} AS t\nGROUP BY {group}\n" +
                $"HAVING {having}\nORDER BY MIN(t.{key.Sql})");
            return rows.ToDictionary(r => ResultSql.CellText(r[0]),
                r => text.Where((_, i) => Convert.ToInt32(r[1 + i], CultureInfo.InvariantCulture) == 1).Select(c => c.Name).ToList());
        }

        /// <summary>
        /// "; differ only in case or trailing spaces, in Label and notes", or nothing for an exact copy.
        /// </summary>
        internal static string LooseNote(List<string>? columns) =>
            columns is { Count: > 0 } ? $"; differ only in {LooseDifference}, in {ResultSql.JoinAnd(columns)}" : "";
    }
}
