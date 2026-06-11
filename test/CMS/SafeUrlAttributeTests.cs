using System.ComponentModel.DataAnnotations;
using Areas.CMS.Validation;

namespace Viper.test.CMS;

/// <summary>
/// Tests for SafeUrlAttribute, in both absolute-only mode (link collections) and
/// AllowRelative mode (left nav item URLs).
/// </summary>
public sealed class SafeUrlAttributeTests
{
    private static ValidationResult? Validate(string? url, bool allowRelative)
    {
        var attribute = new SafeUrlAttribute { AllowRelative = allowRelative };
        return attribute.GetValidationResult(url, new ValidationContext(new object()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com/page")]
    [InlineData("http://example.com")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("tel:+15305551234")]
    public void AbsoluteMode_AllowsSafeSchemesAndEmpty(string? url)
    {
        Assert.Equal(ValidationResult.Success, Validate(url, allowRelative: false));
    }

    [Theory]
    [InlineData("javascript:alert(document.cookie)")]
    [InlineData("JAVASCRIPT:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("java\tscript:alert(1)")]
    [InlineData("java\nscript:alert(1)")]
    public void BothModes_RejectDangerousUrls(string url)
    {
        Assert.NotEqual(ValidationResult.Success, Validate(url, allowRelative: false));
        Assert.NotEqual(ValidationResult.Success, Validate(url, allowRelative: true));
    }

    [Theory]
    [InlineData("/CMS/files")]
    [InlineData("~/ManageLeftNav")]
    [InlineData("page.cfm?x=1")]
    [InlineData("path/to/page#section")]
    public void RelativeMode_AllowsRelativeUrls(string url)
    {
        Assert.Equal(ValidationResult.Success, Validate(url, allowRelative: true));
    }

    [Fact]
    public void AbsoluteMode_RejectsRelativeUrls()
    {
        Assert.NotEqual(ValidationResult.Success, Validate("/CMS/files", allowRelative: false));
    }

    [Fact]
    public void RelativeMode_RejectsColonInFirstSegment()
    {
        Assert.NotEqual(ValidationResult.Success, Validate("foo:bar/baz", allowRelative: true));
    }

    [Fact]
    public void RelativeMode_AllowsColonAfterFirstSegment()
    {
        Assert.Equal(ValidationResult.Success, Validate("path/page:1", allowRelative: true));
    }
}
