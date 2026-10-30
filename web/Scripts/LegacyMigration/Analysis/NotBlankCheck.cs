using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class NotBlankDeclaration : CheckDeclaration
    {
        public required List<string> Columns { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var columns = binder.Columns(where, "columns", table, Columns);
            if (table == null || columns == null)
            {
                return null;
            }

            return new NotBlankCheck
            {
                Name = $"notBlank: {string.Join(", ", columns.Select(c => c.Name))}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [table],
                Columns = columns
            };
        }
    }

    /// <summary>
    /// For each column, the rows where it is NULL, or for text, empty or only spaces. Each column is
    /// its own result, with the affected rows by key.
    /// </summary>
    public sealed class NotBlankCheck : DeclaredCheck
    {
        public required List<ColumnRef> Columns { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context) =>
            Columns.Select(c => RunColumn(context, c)).ToList();

        private CheckResult RunColumn(AnalysisContext context, ColumnRef column)
        {
            var result = NewResult($"notBlank: {column.Name}");
            var isNull = $"t.{column.Sql} IS NULL";

            // = '' is also true for values of only spaces, since SQL Server ignores trailing spaces.
            var isBlank = column.IsText ? $"{column.ComparableSql("t")} = ''" : "1 = 0";
            var counts = context.Database.QueryRows(
                $"SELECT COUNT_BIG(CASE WHEN {isNull} THEN 1 END), COUNT_BIG(CASE WHEN {isBlank} THEN 1 END)\nFROM {column.Table.Name} AS t").Single();
            var nulls = Convert.ToInt64(counts[0], CultureInfo.InvariantCulture);
            var blanks = Convert.ToInt64(counts[1], CultureInfo.InvariantCulture);

            result.Counts.Add(new ResultCount("NULL rows", nulls));
            if (column.IsText)
            {
                result.Counts.Add(new ResultCount("Blank rows (empty or only spaces)", blanks));
            }
            if (nulls + blanks > 0)
            {
                ResultSql.AddAffectedRows(context, result, column.Table, $"{isNull} OR {isBlank}");
                result.Status = CheckStatus.ProblemsFound;
            }
            return result;
        }
    }
}
