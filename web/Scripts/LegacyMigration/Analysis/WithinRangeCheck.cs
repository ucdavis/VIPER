using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class WithinRangeDeclaration : CheckDeclaration
    {
        private static readonly string[] DateFormats = ["yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss"];

        public required string Column { get; init; }
        public JsonElement? Min { get; init; }
        public JsonElement? Max { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var column = binder.Column(where, "column", table, Column);
            if (column == null)
            {
                return null;
            }

            // Text is rejected because it compares alphabetically: "9" sorts after "10".
            if (!column.IsNumber && !column.IsDate)
            {
                binder.Error(where, $"{column.Name} is {column.Summary.Type}; withinRange needs a number or date column.");
                return null;
            }
            var minOk = TryBound(binder, where, "min", Min, column, out var min);
            var maxOk = TryBound(binder, where, "max", Max, column, out var max);
            if (!minOk || !maxOk)
            {
                return null;
            }
            if (min == null && max == null)
            {
                binder.Error(where, "give \"min\", \"max\", or both.");
                return null;
            }
            if (min is IComparable low && max != null && low.CompareTo(max) > 0)
            {
                binder.Error(where, "\"min\" is greater than \"max\".");
                return null;
            }

            return new WithinRangeCheck
            {
                Name = $"withinRange: {column.Name}, {WithinRangeCheck.RangeText(min, max)}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [column.Table],
                Column = column,
                Min = min,
                Max = max
            };
        }

        /// <summary>
        /// A bound as a decimal for a number column or a DateTime for a date column; null when not given.
        /// Returns false, with an error, when it is given in the wrong form.
        /// </summary>
        private static bool TryBound(SchemaBinder binder, string where, string field, JsonElement? element, ColumnRef column, out object? bound)
        {
            bound = null;
            if (element is not { } value || value.ValueKind == JsonValueKind.Null)
            {
                return true;
            }
            if (column.IsNumber && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                bound = number;
                return true;
            }
            if (column.IsDate && value.ValueKind == JsonValueKind.String
                && DateTime.TryParseExact(value.GetString(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                bound = date;
                return true;
            }

            binder.Error(where, column.IsNumber
                ? $"\"{field}\" must be a number, since {column.Name} is {column.Summary.Type}."
                : $"\"{field}\" must be a date written as \"2000-01-01\", since {column.Name} is {column.Summary.Type}.");
            return false;
        }
    }

    /// <summary>
    /// The rows whose value falls below min or above max; both limits are inclusive, and NULLs are
    /// skipped. The affected rows are listed by key. The lowest and highest values found outside the
    /// range are shown only when the column's values can be (ResultSql.CanShow), since a value next to
    /// its row's key belongs to that row (a student's score, for example).
    /// </summary>
    public sealed class WithinRangeCheck : DeclaredCheck
    {
        public required ColumnRef Column { get; init; }
        public object? Min { get; init; }
        public object? Max { get; init; }

        public static string RangeText(object? min, object? max) => (min, max) switch
        {
            ({ } low, { } high) => $"{Format(low)} to {Format(high)}",
            ({ } low, null) => $"at least {Format(low)}",
            _ => $"at most {Format(max)}"
        };

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var result = NewResult(Name);
            var c = $"t.{Column.Sql}";
            var parameters = new List<SqlParameter>();
            var below = Condition(c, "<", "@min", Min, parameters);
            var above = Condition(c, ">", "@max", Max, parameters);

            var counts = context.Database.QueryRows(
                $"SELECT COUNT_BIG(CASE WHEN {below} THEN 1 END), COUNT_BIG(CASE WHEN {above} THEN 1 END),\n" +
                $"    MIN(CASE WHEN {below} THEN {c} END), MAX(CASE WHEN {above} THEN {c} END)\nFROM {Column.Table.Name} AS t",
                Clone(parameters)).Single();
            var belowRows = Convert.ToInt64(counts[0], CultureInfo.InvariantCulture);
            var aboveRows = Convert.ToInt64(counts[1], CultureInfo.InvariantCulture);

            if (Min != null)
            {
                result.Counts.Add(new ResultCount($"Rows below {Format(Min)}", belowRows));
            }
            if (Max != null)
            {
                result.Counts.Add(new ResultCount($"Rows above {Format(Max)}", aboveRows));
            }
            if (belowRows + aboveRows == 0)
            {
                return [result];
            }

            ResultSql.AddAffectedRows(context, result, Column.Table, $"{below} OR {above}", Clone(parameters));
            var found = new[] { (Rows: belowRows, Label: "Lowest below the range", Value: counts[2]), (Rows: aboveRows, Label: "Highest above the range", Value: counts[3]) }
                .Where(f => f.Rows > 0)
                .ToList();
            if (found.TrueForAll(f => ResultSql.CanShow(Column, f.Value)))
            {
                result.Details.Add(string.Join("; ", found.Select(f => $"{f.Label}: {Format(f.Value)}")));
            }
            else
            {
                found.Where(f => !ResultSql.CanShow(Column, f.Value)).ToList().ForEach(f => Column.NoteMasked(f.Value));
            }
            result.Status = CheckStatus.ProblemsFound;
            return [result];
        }

        /// <summary>
        /// The comparison for one limit, adding its parameter; false (1 = 0) when the limit is not given.
        /// </summary>
        private static string Condition(string column, string op, string name, object? bound, List<SqlParameter> parameters)
        {
            if (bound == null)
            {
                return "1 = 0";
            }
            parameters.Add(bound is DateTime date
                ? new SqlParameter(name, SqlDbType.DateTime2) { Value = date }
                : new SqlParameter(name, bound));
            return $"{column} {op} {name}";
        }

        // A parameter belongs to one command, so each query gets its own copies.
        private static SqlParameter[] Clone(List<SqlParameter> parameters) =>
            parameters.Select(p => new SqlParameter(p.ParameterName, p.SqlDbType) { Value = p.Value }).ToArray();

        private static string Format(object? value) => value switch
        {
            DateTime d when d.TimeOfDay == TimeSpan.Zero => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTimeOffset d => d.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
            _ => ResultSql.CellText(value)
        };
    }
}
