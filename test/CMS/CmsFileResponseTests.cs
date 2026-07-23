using Microsoft.AspNetCore.Http;
using Viper.Areas.CMS.Constants;
using Viper.Areas.CMS.Services;

namespace Viper.test.CMS;

/// <summary>
/// Tests for the /CMS/Files download-response hardening: X-Content-Type-Options: nosniff on every
/// file response, and forcing inline-unsafe types (html/svg) to download instead of rendering in
/// the app origin (stored-XSS guard). Legacy .html files stay downloadable, just never inline.
/// </summary>
public sealed class CmsFileResponseTests
{
    [Fact]
    public void SetNoSniff_SetsHeader()
    {
        var response = new DefaultHttpContext().Response;

        CmsFileResponse.SetNoSniff(response);

        Assert.Equal("nosniff", response.Headers["X-Content-Type-Options"]);
    }

    [Theory]
    [InlineData("page.html")]
    [InlineData("PAGE.HTM")]
    [InlineData("doc.xhtml")]
    [InlineData("logo.svg")]
    [InlineData(@"S:\Files\cats\evil.HTML")]
    public void DispositionType_InlineUnsafe_ForcesAttachment(string fileName)
    {
        Assert.Equal("attachment", CmsFileResponse.DispositionType(fileName));
        Assert.True(CmsFileTypes.ShouldForceAttachment(fileName));
    }

    [Theory]
    [InlineData("report.pdf")]
    [InlineData("image.png")]
    [InlineData("notes.txt")]
    [InlineData("sheet.xlsx")]
    [InlineData("noextension")]
    public void DispositionType_SafeTypes_StayInline(string fileName)
    {
        Assert.Equal("inline", CmsFileResponse.DispositionType(fileName));
        Assert.False(CmsFileTypes.ShouldForceAttachment(fileName));
    }
}
