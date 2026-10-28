using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    /// <summary>
    /// The fields matchesPattern and doesNotMatchPattern share: a text column and SQL Server LIKE patterns.
    /// </summary>
    public abstract class PatternDeclaration : CheckDeclaration
    {
        public required string Column { get; init; }
        public required List<string> Patterns { get; init; }

        protected abstract bool MustMatch { get; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var column = binder.Column(where, "column", table, Column);

            if (Patterns.Count == 0 || Patterns.Exists(string.IsNullOrEmpty))
            {
                binder.Error(where, "\"patterns\" must list at least one LIKE pattern, none empty.");
                return null;
            }
            if (column == null)
            {
                return null;
            }

            // A number or date column has no text format to test; withinRange and canConvertTo cover them.
            if (!column.IsText)
            {
                binder.Error(where, $"{column.Name} is {column.Summary.Type}; {Check} checks text columns.");
                return null;
            }

            return new MatchesPatternCheck
            {
                Name = $"{Check}: {column.Name}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [column.Table],
                Column = column,
                Patterns = Patterns,
                MustMatch = MustMatch
            };
        }
    }

    public sealed class MatchesPatternDeclaration : PatternDeclaration
    {
        protected override bool MustMatch => true;
    }

    public sealed class DoesNotMatchPatternDeclaration : PatternDeclaration
    {
        protected override bool MustMatch => false;
    }

    /// <summary>
    /// The rows of a text column whose value matches none of the patterns (matchesPattern), or matches any
    /// of them (doesNotMatchPattern), by SQL Server's LIKE under the column's collation. NULLs and blanks are
    /// ignored, and char and nchar padding is trimmed; other trailing spaces count. The values found are
    /// shown when the column's values can be (ResultSql.CanShow), by shape otherwise, and the rows are
    /// listed by key.
    /// </summary>
    public sealed class MatchesPatternCheck : DeclaredCheck
    {
        public required ColumnRef Column { get; init; }
        public required IReadOnlyList<string> Patterns { get; init; }
        public required bool MustMatch { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var result = NewResult(Name);
            var e = Column.IsPadded ? $"RTRIM({Column.ComparableSql("t")})" : Column.ComparableSql("t");
            var matches = Patterns.Select((_, i) => $"{e} LIKE @pattern{i}").ToList();
            var anyMatch = string.Join(" OR ", matches);
            var found = $"{e} <> '' AND {(MustMatch ? $"NOT ({anyMatch})" : $"({anyMatch})")}";
            var label = MustMatch ? "that match no pattern" : "that match a pattern";

            var values = context.Database.QueryRows(
                $"SELECT TOP ({AnalysisContext.ExampleLimit}) {e}, COUNT_BIG(*), COUNT_BIG(*) OVER ()\nFROM {Column.Table.Name} AS t\n" +
                $"WHERE {found}\nGROUP BY {e}\nORDER BY COUNT_BIG(*) DESC", Parameters());
            var distinct = values.Count > 0 ? Convert.ToInt64(values[0][2], CultureInfo.InvariantCulture) : 0;
            if (distinct == 0)
            {
                result.Counts.Add(new ResultCount($"Rows {label}", 0));
                return [result];
            }

            var rows = ResultSql.AddAffectedRows(context, result, Column.Table, found, Parameters());
            result.Counts.Add(new ResultCount($"Rows {label}", rows));
            result.Counts.Add(new ResultCount($"Distinct values {label}", distinct));
            if (!MustMatch && Patterns.Count > 1)
            {
                // A value can match several patterns, so these can add up to more than the rows.
                var perPattern = context.Database.QueryRows(
                    $"SELECT {string.Join(", ", matches.Select(m => $"SUM(CASE WHEN {m} THEN 1 ELSE 0 END)"))}\n" +
                    $"FROM {Column.Table.Name} AS t\nWHERE {found}", Parameters()).Single();
                result.Counts.AddRange(Patterns.Select((p, i) =>
                    new ResultCount($"Rows matching {p}", Convert.ToInt64(perPattern[i], CultureInfo.InvariantCulture))));
            }

            var shown = distinct > values.Count ? $" ({values.Count:N0} of {distinct:N0})" : "";
            if (values.TrueForAll(v => ResultSql.CanShow(Column, v[0])))
            {
                var listed = values.Select(v => $"{ResultSql.ShowValue(v[0])} ({Convert.ToInt64(v[1], CultureInfo.InvariantCulture):N0})");
                result.Details.Add($"Values{shown}: {string.Join(", ", listed)}");
            }
            else
            {
                values.Where(v => !ResultSql.CanShow(Column, v[0])).ToList().ForEach(v => Column.NoteMasked(v[0]));
                var shapes = values.Select(v => Convert.ToString(v[0], CultureInfo.InvariantCulture) ?? "").ToList();
                result.Details.Add($"Values{shown}, by shape: {ResultSql.ShapeSummary(shapes)}");
            }
            result.Status = CheckStatus.ProblemsFound;
            return [result];
        }

        /// <summary>
        /// New each time: a parameter stays attached to the command that used it.
        /// </summary>
        private SqlParameter[] Parameters() =>
            Patterns.Select((p, i) => new SqlParameter($"@pattern{i}", p)).ToArray();
    }
}
