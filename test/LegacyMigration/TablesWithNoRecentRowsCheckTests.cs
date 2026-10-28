using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

[InvariantCulture]
public class TablesWithNoRecentRowsCheckTests
{
    [Theory]
    [InlineData(2024, true)]
    [InlineData(2012, true)]
    [InlineData(2025, false)]
    [InlineData(2026, false)]
    [InlineData(9999, false)]
    public void IsOld_TwoOrMoreYearsBeforeTheRun_IsOld(int latestYear, bool expected)
    {
        Assert.Equal(expected, TablesWithNoRecentRowsCheck.IsOld(latestYear, 2026));
    }

    [Fact]
    public void IsOld_NoDates_IsNotOld()
    {
        Assert.False(TablesWithNoRecentRowsCheck.IsOld(null, 2026));
    }

    [Fact]
    public void Run_OldAndEmptyTables_ShowTheirLatestYearOrEmpty()
    {
        var plan = TestSchema.Plan("""{ "dateColumns": { "[dbo].[visitor]": "addDate" } }""");
        var database = ProfileRow.Answering(plan,
            new ProfileRow(plan.Schema.FindTable(TestSchema.Visitor)!, 277).Date("addDate", latest: 2015),
            new ProfileRow(plan.Schema.FindTable("[dbo].[Status]")!, 0));

        var result = Assert.Single(new TablesWithNoRecentRowsCheck().Run(TestSchema.Context(plan, database)));

        Assert.Equal([["[dbo].[Status]", "-", "empty", "0"], ["[dbo].[visitor]", "addDate", "2015", "277"]], result.Grids[0].Rows);
    }
}
