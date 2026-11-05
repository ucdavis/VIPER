using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

[InvariantCulture]
public class TablesThatLookLikeCopiesCheckTests
{
    private static (string Name, IReadOnlyList<ColumnSummary> Columns) Table(string name, params string[] columns) =>
        (name, columns.Select(c =>
        {
            var parts = c.Split(' ');
            return new ColumnSummary(parts[0], parts.Length > 1 ? parts[1] : "int", null, null, true, false, null, null);
        }).ToList());

    [Fact]
    public void Groups_ExactCopyWithDifferentCase_IsGrouped()
    {
        var groups = TablesThatLookLikeCopiesCheck.Groups(
        [
            Table("[dbo].[studentSchedule]", "ID", "Mothra_ID varchar(8)", "Week_ID"),
            Table("[dbo].[scheduleWithSeq001]", "id", "MOTHRA_ID varchar(8)", "week_id")
        ]);

        Assert.Equal([["[dbo].[scheduleWithSeq001]", "[dbo].[studentSchedule]"]], groups);
    }

    [Fact]
    public void Groups_TwoExtraColumns_MatchesButThreeDoNot()
    {
        var groups = TablesThatLookLikeCopiesCheck.Groups(
        [
            Table("[dbo].[a]", "ID", "Name varchar(50)", "Code"),
            Table("[dbo].[a_two_more]", "ID", "Name varchar(50)", "Code", "X", "Y"),
            Table("[dbo].[a_three_more]", "ID", "Name varchar(50)", "Code", "X1", "Y1", "Z1")
        ]);

        Assert.Equal([["[dbo].[a]", "[dbo].[a_two_more]"]], groups);
    }

    [Fact]
    public void Groups_DifferentTypeOrLength_DoNotMatch()
    {
        var groups = TablesThatLookLikeCopiesCheck.Groups(
        [
            Table("[dbo].[a]", "ID", "Name varchar(50)", "Code"),
            Table("[dbo].[b]", "ID", "Name varchar(60)", "Code"),
            Table("[dbo].[c]", "ID", "Name varchar(50)", "Code bigint")
        ]);

        Assert.Empty(groups);
    }

    [Fact]
    public void Groups_FewerThanThreeColumns_AreLeftOut()
    {
        var groups = TablesThatLookLikeCopiesCheck.Groups(
        [
            Table("[dbo].[link_a]", "A_ID", "B_ID"),
            Table("[dbo].[link_b]", "A_ID", "B_ID")
        ]);

        Assert.Empty(groups);
    }

    [Fact]
    public void Groups_ChainedMatches_FormOneGroup()
    {
        // b has 2 more than a, and c 2 more than b: c has 4 more than a, but the chain joins them.
        var groups = TablesThatLookLikeCopiesCheck.Groups(
        [
            Table("[dbo].[c]", "ID", "P", "Q", "R", "S", "T", "U"),
            Table("[dbo].[a]", "ID", "P", "Q"),
            Table("[dbo].[b]", "ID", "P", "Q", "R", "S"),
            Table("[dbo].[unrelated]", "Other", "Columns", "Here")
        ]);

        Assert.Equal([["[dbo].[a]", "[dbo].[b]", "[dbo].[c]"]], groups);
    }

    [Fact]
    public void Groups_SameCountButDifferentColumns_DoNotMatch()
    {
        var groups = TablesThatLookLikeCopiesCheck.Groups(
        [
            Table("[dbo].[a]", "ID", "P", "Q"),
            Table("[dbo].[b]", "ID", "P", "R")
        ]);

        Assert.Empty(groups);
    }
}
