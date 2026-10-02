using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportKeyTests
{
    [Theory]
    [InlineData("personnel.employees-on-leave")]
    [InlineData("a.b")]
    [InlineData("clinical-scheduler.rotation-2")]
    [InlineData("a1.b2")]
    public void IsValid_WellFormedKey_ReturnsTrue(string key)
    {
        Assert.True(ReportKey.IsValid(key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nodot")]
    [InlineData(".report")]
    [InlineData("area.")]
    [InlineData("a.b.c")]
    [InlineData("-a.b")]
    [InlineData("a-.b")]
    [InlineData("a.-b")]
    [InlineData("a.b-")]
    [InlineData("Personnel.report")]
    [InlineData("a.b_c")]
    [InlineData("a.b c")]
    public void IsValid_MalformedKey_ReturnsFalse(string? key)
    {
        Assert.False(ReportKey.IsValid(key));
    }
}
