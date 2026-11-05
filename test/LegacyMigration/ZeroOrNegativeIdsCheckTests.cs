using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

[InvariantCulture]
public class ZeroOrNegativeIdsCheckTests
{
    [Theory]
    [InlineData("ID")]
    [InlineData("Didactic_ID")]
    [InlineData("course_id")]
    [InlineData("potentialScheduleID")]
    [InlineData("CourseId")]
    [InlineData("effort_CourseID")]
    public void IsIdName_NameEndingInIdAsAWord_Matches(string name)
    {
        Assert.True(ZeroOrNegativeIdsCheck.IsIdName(name));
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("valid")]
    [InlineData("Void")]
    [InlineData("Idle")]
    [InlineData("Hours")]
    public void IsIdName_OtherNames_DoNotMatch(string name)
    {
        Assert.False(ZeroOrNegativeIdsCheck.IsIdName(name));
    }
}
