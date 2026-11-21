using Viper.Scripts.LegacyMigration;
using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

[InvariantCulture]
public class PlaceholderTextCheckTests
{
    [Theory]
    [InlineData("Unknown")]
    [InlineData("unknown")]
    [InlineData("  N/A ")]
    [InlineData("na")]
    [InlineData("TBA")]
    [InlineData("Test")]
    [InlineData("?")]
    [InlineData("-")]
    public void IsWord_PlaceholderAsTheWholeValue_Matches(string value)
    {
        Assert.True(PlaceholderTextCheck.IsWord(value));
    }

    [Theory]
    [InlineData("Unknown donor")]
    [InlineData("Testing")]
    [InlineData("N/A - see notes")]
    [InlineData("NAH")]
    [InlineData("")]
    public void IsWord_WordInsideTextOrOtherValues_DoesNotMatch(string value)
    {
        Assert.False(PlaceholderTextCheck.IsWord(value));
    }

    [Fact]
    public void Where_ComparesTheTrimmedValueWithEveryWord_IgnoringCase()
    {
        var summary = new SchemaSummary
        {
            Database = "ClinicalScheduler",
            Tables =
            [
                new TableSummary("[dbo].[InstructorSchedule]", 10, true, true, 0, ["ID"], [],
                    [new ColumnSummary("role", "varchar(50)", null, 50, true, false, null, null)])
            ]
        };
        var column = new SchemaIndex(summary, new HashSet<string>()).FindTable("[dbo].[InstructorSchedule]")!.Columns[0];

        var where = PlaceholderTextCheck.Where(column);

        Assert.StartsWith("LTRIM(RTRIM(t.[role])) COLLATE Latin1_General_CI_AS IN (", where, StringComparison.Ordinal);
        Assert.All(PlaceholderTextCheck.Words, w => Assert.Contains($"'{w}'", where, StringComparison.Ordinal));
    }
}
