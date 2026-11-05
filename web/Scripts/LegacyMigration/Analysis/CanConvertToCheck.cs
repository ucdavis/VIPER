using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class CanConvertToDeclaration : CheckDeclaration
    {
        // decimal needs its precision and scale: a bare decimal is decimal(18, 0), which rounds fractions
        // instead of failing on them.
        private static readonly Regex TypePattern = new(@"^(int|bigint|date|datetime|bit|decimal\(\s*(\d+)\s*,\s*(\d+)\s*\))$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

        public required string Column { get; init; }
        public required string Type { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var column = binder.Column(where, "column", table, Column);

            var match = TypePattern.Match(Type.Trim());
            if (!match.Success)
            {
                binder.Error(where, $"\"type\" \"{Type}\" is not supported. Use int, bigint, date, datetime, bit, or decimal(precision, scale), such as decimal(10, 2).");
            }
            else if (match.Groups[2].Success && !ValidDecimal(match.Groups[2].Value, match.Groups[3].Value))
            {
                binder.Error(where, $"\"type\" \"{Type}\": precision must be 1 to 38, and scale 0 to the precision.");
                return null;
            }
            if (column == null || !match.Success)
            {
                return null;
            }

            // A number or date column converts silently (1.5 becomes 1 as int), so only text is checked.
            if (!column.IsText)
            {
                binder.Error(where, $"{column.Name} is {column.Summary.Type}; canConvertTo checks text columns.");
                return null;
            }

            var type = Regex.Replace(Type.Trim().ToLowerInvariant(), @"\s+", "", RegexOptions.None, TimeSpan.FromSeconds(1)).Replace(",", ", ");
            return new CanConvertToCheck
            {
                Name = $"canConvertTo: {column.Name} as {type}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [column.Table],
                Column = column,
                Type = type
            };
        }

        private static bool ValidDecimal(string precision, string scale) =>
            int.TryParse(precision, NumberStyles.None, CultureInfo.InvariantCulture, out var p)
            && int.TryParse(scale, NumberStyles.None, CultureInfo.InvariantCulture, out var s)
            && p is >= 1 and <= 38 && s <= p;
    }

    /// <summary>
    /// The rows of a text column whose value would not convert to a type, by SQL Server's own conversion
    /// (TRY_CAST), so the result matches what a conversion in SQL would do. NULLs and blanks are ignored.
    /// The values that fail are shown when the column's values can be (ResultSql.CanShow), by shape
    /// otherwise, and the rows are listed by key.
    /// </summary>
    public sealed class CanConvertToCheck : DeclaredCheck
    {
        public required ColumnRef Column { get; init; }
        public required string Type { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var result = NewResult(Name);
            var e = Column.ComparableSql("t");
            var fails = $"{e} <> '' AND TRY_CAST({e} AS {Type}) IS NULL";
            var values = context.Database.QueryRows(
                $"SELECT TOP ({AnalysisContext.ExampleLimit}) {e}, COUNT_BIG(*), COUNT_BIG(*) OVER ()\nFROM {Column.Table.Name} AS t\n" +
                $"WHERE {fails}\nGROUP BY {e}\nORDER BY COUNT_BIG(*) DESC");
            var distinct = values.Count > 0 ? Convert.ToInt64(values[0][2], CultureInfo.InvariantCulture) : 0;
            if (distinct == 0)
            {
                result.Counts.Add(new ResultCount("Rows that will not convert", 0));
                return [result];
            }

            var rows = ResultSql.AddAffectedRows(context, result, Column.Table, fails);
            result.Counts.Add(new ResultCount("Rows that will not convert", rows));
            result.Counts.Add(new ResultCount("Distinct values that will not convert", distinct));
            if (values.TrueForAll(v => ResultSql.CanShow(Column, v[0])))
            {
                var listed = values.Select(v => $"{ResultSql.ShowValue(Column, v[0])} ({Convert.ToInt64(v[1], CultureInfo.InvariantCulture):N0})");
                result.Details.Add($"Values{(distinct > values.Count ? $" ({values.Count:N0} of {distinct:N0})" : "")}: {string.Join(", ", listed)}");
            }
            else
            {
                values.Where(v => !ResultSql.CanShow(Column, v[0])).ToList().ForEach(v => Column.NoteMasked(v[0]));
                var shapes = values.Select(v => Convert.ToString(v[0], CultureInfo.InvariantCulture)?.Trim() ?? "").ToList();
                result.Details.Add($"Values{(distinct > values.Count ? $" ({values.Count:N0} of {distinct:N0})" : "")}, by shape: {ResultSql.ShapeSummary(shapes)}");
            }
            result.Status = CheckStatus.ProblemsFound;
            return [result];
        }
    }
}
