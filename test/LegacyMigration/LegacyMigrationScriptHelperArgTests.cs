using Viper.Scripts.LegacyMigration;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The command-line arguments the launchers pass: flags as "--name value" or "--name=value", environment
/// names in any case, and connection names that become folder names.
/// </summary>
public class LegacyMigrationScriptHelperArgTests
{
    private static string[] Args(string commandLine) => commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    [Theory]
    [InlineData("--connection SIS")]
    [InlineData("--connection=SIS")]
    [InlineData("--left Test --CONNECTION SIS")]
    public void GetRequiredArg_EitherForm_ReturnsTheValue(string commandLine)
    {
        Assert.Equal("SIS", LegacyMigrationScriptHelper.GetRequiredArg(Args(commandLine), "connection"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("--left Test")]
    [InlineData("--connection")]
    public void GetRequiredArg_MissingFlagOrValue_SaysWhichFlag(string commandLine)
    {
        var ex = Assert.Throws<ArgumentException>(() => LegacyMigrationScriptHelper.GetRequiredArg(Args(commandLine), "connection"));

        Assert.Equal("Missing required --connection <value> argument.", ex.Message);
    }

    [Theory]
    [InlineData(new[] { "--project", "CareerSelection" }, "CareerSelection")]
    [InlineData(new[] { "--project=" }, null)]
    [InlineData(new[] { "--project", " " }, null)]
    [InlineData(new[] { "--connection", "SIS" }, null)]
    public void GetOptionalArg_AbsentOrBlank_IsNull(string[] args, string? expected)
    {
        Assert.Equal(expected, LegacyMigrationScriptHelper.GetOptionalArg(args, "project"));
    }

    [Theory]
    [InlineData("test", "Test")]
    [InlineData("PRODUCTION", "Production")]
    [InlineData("Development", "Development")]
    public void ParseEnvironment_AnyCase_GivesTheCanonicalName(string value, string expected)
    {
        Assert.Equal(expected, LegacyMigrationScriptHelper.ParseEnvironment(value));
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseEnvironment_UnknownName_IsRefused(string? value)
    {
        var ex = Assert.Throws<ArgumentException>(() => LegacyMigrationScriptHelper.ParseEnvironment(value));

        Assert.StartsWith("Environment must be one of Development, Test, Production", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ClinicalScheduler")]
    [InlineData("SIS_2")]
    public void ValidateConnectionName_PlainIdentifier_IsAccepted(string name)
    {
        Assert.Equal(name, LegacyMigrationScriptHelper.ValidateConnectionName(name));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("SIS/Other")]
    [InlineData("Clinical Scheduler")]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateConnectionName_AnythingElse_IsRefused(string? name)
    {
        Assert.Throws<ArgumentException>(() => LegacyMigrationScriptHelper.ValidateConnectionName(name));
    }
}
