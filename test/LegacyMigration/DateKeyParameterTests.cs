using System.Data;
using Microsoft.Data.SqlClient;
using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Each check that looks rows up again by values it read must send a date as its column's own type: untyped,
/// a DateTime goes as datetime, which rounds to 3 ms and fails before 1753, so the rows would be missed. One
/// test per check that does so, so a check that stops passing its columns fails here, not only in the helper's
/// own tests (ResultSqlParameterTests).
/// </summary>
[InvariantCulture]
public class DateKeyParameterTests
{
    private static readonly DateTime StartsAt = new DateTime(2024, 5, 1, 8, 30, 0, 123, DateTimeKind.Local).AddTicks(4567);
    private static readonly DateTime Day = new(1, 1, 1, 0, 0, 0, DateTimeKind.Local);

    /// <summary>
    /// The test schema with [dbo].[Shifts]: an identity, a datetime2(3), a date, and a code.
    /// </summary>
    private static AnalysisPlan Plan(string check)
    {
        var summary = TestSchema.Summary();
        summary.Tables.Add(new TableSummary("[dbo].[Shifts]", 10, true, true, 0, ["id"], [],
        [
            new ColumnSummary("id", "int", null, null, false, false, null, new IdentitySummary("1", "1", "10")),
            new ColumnSummary("startsAt", "datetime2(3)", null, null, true, false, null, null),
            new ColumnSummary("day", "date", null, null, true, false, null, null),
            new ColumnSummary("code", "varchar(10)", null, 10, true, false, null, null)
        ]));
        return TestSchema.Plan($$"""{ "checks": [{{check}}] }""", summary);
    }

    private static SqlParameter ParameterFor(FakeDatabase database, object value) =>
        database.Queries.SelectMany(q => q.Parameters).First(p => Equals(p.Value, value));

    [Fact]
    public void OneValuePerKey_DateKey_IsLookedUpAsItsColumnsType()
    {
        var plan = Plan("""{ "check": "oneValuePerKey", "table": "[dbo].[Shifts]", "keyColumns": ["startsAt"], "columns": ["code"] }""");
        var database = new FakeDatabase()
            .Answer("DENSE_RANK()")
            .Answer("HAVING", [StartsAt, 2L, 1L, null, null]);

        Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database));

        var parameter = ParameterFor(database, StartsAt);
        Assert.Equal((SqlDbType.DateTime2, (byte)3), (parameter.SqlDbType, parameter.Scale));
    }

    [Fact]
    public void NoDuplicates_DateColumn_IsLookedUpAsItsColumnsType()
    {
        var plan = Plan("""{ "check": "noDuplicates", "table": "[dbo].[Shifts]", "columns": ["day", "code"] }""");
        // The group's day, code, and code's length, then rows and dates, then a flag per compared column
        // (startsAt, day, code): the rows differ in startsAt, so the group is listed by its rows.
        var database = new FakeDatabase()
            .Answer("COUNT(DISTINCT", [Day, "A", 1, 2L, null, null, 1, 0, 0])
            .Answer("(SELECT CASE WHEN")
            .AnswerWith("SELECT COUNT_BIG(*), SUM(x.n)", _ => [[0L, null, null, null, null, null]]);

        _ = new DuplicateRowsCheck(ignoringSurrogateKey: true).Run(TestSchema.Context(plan, database)).ToList();

        Assert.Equal(SqlDbType.Date, ParameterFor(database, Day).SqlDbType);
    }

    [Fact]
    public void ValuesMustExistIn_WithheldDateValue_IsLookedUpAsItsColumnsType()
    {
        var plan = Plan("""
            { "check": "valuesMustExistIn", "table": "[dbo].[Shifts]", "columns": ["day", "code"], "otherTable": "[dbo].[Shifts]", "otherColumns": ["day", "code"] }
            """);
        // code has no safe origin, so the value is withheld and its rows are looked up by it.
        var database = new FakeDatabase()
            .Answer("SELECT TOP (50) t.[id], COUNT_BIG(*) OVER ()")
            .Answer("GROUP BY t.[day], t.[code]", [Day, "A", 2L, 1L, 2L, null, null]);

        Assert.Single(plan.DeclaredChecks).Run(TestSchema.Context(plan, database));

        Assert.Equal(SqlDbType.Date, ParameterFor(database, Day).SqlDbType);
    }
}
