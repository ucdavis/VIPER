using System.Collections.Generic;

namespace Viper.Scripts.LegacyMigration.Analysis
{
    public sealed class StartBeforeEndDeclaration : CheckDeclaration
    {
        public required string StartColumn { get; init; }
        public required string EndColumn { get; init; }

        public override DeclaredCheck? Bind(SchemaBinder binder, string where)
        {
            var table = binder.Object(where, "table", Table);
            var start = binder.Column(where, "startColumn", table, StartColumn);
            var end = binder.Column(where, "endColumn", table, EndColumn);
            if (table == null || start == null || end == null)
            {
                return null;
            }

            // Text is rejected because it compares alphabetically: "9" sorts after "10".
            if (!(start.IsDate && end.IsDate) && !(start.IsNumber && end.IsNumber))
            {
                binder.Error(where, $"{start.Name} is {start.Summary.Type} and {end.Name} is {end.Summary.Type}; " +
                                    "startBeforeEnd needs two date columns or two number columns.");
                return null;
            }

            return new StartBeforeEndCheck
            {
                Name = $"startBeforeEnd: {start.Name}, {end.Name}",
                Description = Description,
                Severity = SeverityOrDefault,
                Tables = [table],
                Start = start,
                End = end
            };
        }
    }

    /// <summary>
    /// The rows whose end comes before their start. Rows where either is NULL are skipped; a start equal
    /// to the end is fine.
    /// </summary>
    public sealed class StartBeforeEndCheck : DeclaredCheck
    {
        public required ColumnRef Start { get; init; }
        public required ColumnRef End { get; init; }

        public override IReadOnlyList<CheckResult> Run(AnalysisContext context)
        {
            var result = NewResult(Name);
            var rows = ResultSql.AddAffectedRows(context, result, Start.Table, $"t.{End.Sql} < t.{Start.Sql}");
            result.Counts.Add(new ResultCount("Rows that end before they start", rows));
            result.Status = rows > 0 ? CheckStatus.ProblemsFound : CheckStatus.NoProblemsFound;
            return [result];
        }
    }
}
