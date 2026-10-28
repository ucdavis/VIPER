using Viper.Scripts.LegacyMigration.Analysis;

namespace Viper.test.LegacyMigration;

[InvariantCulture]
public class AuditOnlyDifferencesCheckTests
{
    [Theory]
    [InlineData("AdminAudit")]
    [InlineData("externalRequestAudit")]
    [InlineData("ChangeLog")]
    [InlineData("error_logs")]
    [InlineData("StatusHistory")]
    [InlineData("tblHist")]
    [InlineData("AUDIT_Trail")]
    public void IsLogName_NameWithALogWord_Matches(string name)
    {
        Assert.True(AuditOnlyDifferencesCheck.IsLogName(name));
    }

    [Theory]
    [InlineData("Histology")]
    [InlineData("Histopathology")]
    [InlineData("Login")]
    [InlineData("Logistics")]
    [InlineData("Auditorium")]
    [InlineData("Service")]
    public void IsLogName_NameWithAWordOnlyStartingLikeOne_DoesNotMatch(string name)
    {
        Assert.False(AuditOnlyDifferencesCheck.IsLogName(name));
    }
}
