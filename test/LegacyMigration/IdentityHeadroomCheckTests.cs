using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Identity headroom reads only the export: each identity column's last value against its type's range.
/// </summary>
[InvariantCulture]
public class IdentityHeadroomCheckTests
{
    private static TableSummary Table(string name, string? lastValue) =>
        new(name, 230, true, false, 0, ["id"], [],
            [new ColumnSummary("id", "tinyint", null, null, false, false, null, new IdentitySummary("1", "1", lastValue))]);

    [Fact]
    public void Run_ColumnNearItsLimit_IsAWarningAndHeadsTheMostUsed()
    {
        var summary = new SchemaSummary
        {
            Database = "Legacy",
            HasViewDefinition = true,
            Tables = [Table("[dbo].[codes]", "230"), Table("[dbo].[unused]", null)]
        };
        var plan = TestSchema.Plan("{}", summary);

        var results = new IdentityHeadroomCheck().Run(TestSchema.Context(plan, new FakeDatabase())).ToList();

        var warning = Assert.Single(results, r => r.Status == CheckStatus.ProblemsFound);
        Assert.Equal(["id (tinyint): last value 230, 90.2% of its range used."], warning.Details);
        var mostUsed = Assert.Single(Assert.Single(results, r => r.Kind == ResultKind.Context).Grids);
        Assert.Equal([["[dbo].[codes]", "id", "tinyint", "230", "90.2%"]], mostUsed.Rows);
    }
}
