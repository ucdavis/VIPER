using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class SameKeysAsDeclaration : CheckDeclaration
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

            return new SameKeysAsCheck
            {
                Name = "sameKeysAs",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [table, other],
                Columns = columns,
                OtherColumns = otherColumns
            };
        }
    }

    /// <summary>
    /// The distinct keys found in only one of two tables, both ways round. Keys with a NULL in any column
    /// are skipped, as SQL Server does for a multi-column foreign key.
    /// </summary>
    public sealed class SameKeysAsCheck : DeclaredCheck
    {
        public required List<ColumnRef> Columns { get; init; }
        public required List<ColumnRef> OtherColumns { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var table = Tables[0];
            var other = Tables[1];
            var result = NewResult(Name);

            var onlyHere = AddOnlyIn(context, result, Columns, OtherColumns);
            var onlyThere = AddOnlyIn(context, result, OtherColumns, Columns);
            result.Counts.Add(new ResultCount($"Keys only in {table.Name}", onlyHere));
            result.Counts.Add(new ResultCount($"Keys only in {other.Name}", onlyThere));
            result.Status = onlyHere + onlyThere > 0 ? CheckStatus.ProblemsFound : CheckStatus.NoProblemsFound;
            return [result];
        }

        /// <summary>
        /// The keys of one side with no match on the other: their count, and up to ExampleLimit of them as
        /// a grid, with the date range of their rows from that side's own date column.
        /// </summary>
        private static long AddOnlyIn(AnalysisContext context, CheckResult result, List<ColumnRef> columns, List<ColumnRef> otherColumns)
        {
            var table = columns[0].Table;
            var keys = ResultSql.List("a", columns);
            var dates = ResultSql.DateRange(context, table, (aggregate, date) => $"{aggregate}(a.{date})");
            var rows = context.Database.QueryRows(
                $"SELECT TOP ({AnalysisContext.ExampleLimit}) {ResultSql.List("x", columns)}, COUNT_BIG(*) OVER (), MIN(x.Earliest) OVER (), MAX(x.Latest) OVER ()\n" +
                $"FROM (SELECT {keys}, {dates} FROM {table.Name} AS a\n" +
                $"      WHERE {ResultSql.NotNull("a", columns)}\n" +
                $"        AND NOT EXISTS (SELECT 1 FROM {otherColumns[0].Table.Name} AS b WHERE {ResultSql.Equal("a", columns, "b", otherColumns)})\n" +
                $"      GROUP BY {keys}) AS x\n" +
                $"ORDER BY {ResultSql.List("x", columns)}");

            var total = rows.Count > 0 ? Convert.ToInt64(rows[0][columns.Count], CultureInfo.InvariantCulture) : 0;
            if (total > 0)
            {
                var range = ResultSql.RangeText(rows[0][columns.Count + 1], rows[0][columns.Count + 2]);
                result.Grids.Add(new ResultGrid($"Only in {table.Name}", columns.Select(c => c.Name).ToList(), [],
                    rows.Select(r => columns.Select((c, i) => ResultSql.KeyCellText(c, r[i])).ToList()).ToList(), total,
                    range != null ? $"rows dated {range}" : null));
            }
            return total;
        }
    }
}
