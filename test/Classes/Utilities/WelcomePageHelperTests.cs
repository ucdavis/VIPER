using Viper.Classes.Utilities;

namespace Viper.test.Classes.Utilities;

public class WelcomePageHelperTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("/", null)]
    [InlineData("/RAPS/Roles", "RAPS")]
    [InlineData("/Effort/Foo", "Effort Reporting")]
    [InlineData("/ClinicalScheduler/Schedule", "Clinical Scheduler")]
    [InlineData("/CTS/Index", "Competency Tracking System")]
    [InlineData("/Directory/Home", "Directory")]
    [InlineData("/CMS/Page", "CMS")]
    [InlineData("/Home/Policy", "Policy")]
    [InlineData("/raps/roles", "RAPS")]
    [InlineData("~/RAPS/Roles", "RAPS")]
    [InlineData("~/Effort/Foo", "Effort Reporting")]
    public void ResolveDestinationLabel_KnownAndFallbackCases(string? returnUrl, string? expected)
    {
        var actual = WelcomePageHelper.ResolveDestinationLabel(returnUrl);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("http://evil.com")]
    [InlineData("https://evil.com/path")]
    [InlineData("//evil.com/x")]
    [InlineData("//\\evil.com")]
    [InlineData("~//evil.com")]
    [InlineData("~/\\evil.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("RAPS/Roles")]
    public void ResolveDestinationLabel_RejectsNonLocalUrls(string returnUrl)
    {
        Assert.Null(WelcomePageHelper.ResolveDestinationLabel(returnUrl));
    }

    [Theory]
    [InlineData("/RAPS/Roles?id=5", "RAPS")]
    [InlineData("/Effort/Foo#frag", "Effort Reporting")]
    public void ResolveDestinationLabel_StripsQueryAndFragment(string returnUrl, string expected)
    {
        Assert.Equal(expected, WelcomePageHelper.ResolveDestinationLabel(returnUrl));
    }
}
