using Viper.Services;

namespace Viper.test.Services;

/// <summary>
/// Unit tests for HtmlToTextConverter.
/// </summary>
public class HtmlToTextConverterTests
{
    [Fact]
    public void Convert_EmptyHtml_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, HtmlToTextConverter.Convert(""));
        Assert.Equal(string.Empty, HtmlToTextConverter.Convert(null!));
        Assert.Equal(string.Empty, HtmlToTextConverter.Convert("   "));
    }

    [Fact]
    public void Convert_PlainText_ReturnsText()
    {
        var html = "<p>Hello World</p>";
        var result = HtmlToTextConverter.Convert(html);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Convert_Link_FormatsAsTextWithUrl()
    {
        var html = "<a href=\"https://example.com\">Click here</a>";
        var result = HtmlToTextConverter.Convert(html);
        Assert.Equal("Click here (https://example.com)", result);
    }

    [Fact]
    public void Convert_LinkWithUrlAsText_DoesNotDuplicate()
    {
        var html = "<a href=\"https://example.com\">https://example.com</a>";
        var result = HtmlToTextConverter.Convert(html);
        Assert.Equal("https://example.com", result);
    }

    [Fact]
    public void Convert_DataTable_FormatsPipeSeparated()
    {
        var html = @"
            <table class=""data-table"">
                <tr><th>Course</th><th>Hours</th></tr>
                <tr><td>VME 200</td><td>10</td></tr>
            </table>";

        var result = HtmlToTextConverter.Convert(html);

        Assert.Contains("Course | Hours", result);
        Assert.Contains("VME 200 | 10", result);
    }

    [Fact]
    public void Convert_DataTableSingleCellRow_IncludesContent()
    {
        var html = @"
            <table class=""data-table"">
                <tr><th>Course</th><th>Hours</th></tr>
                <tr><td colspan=""2"">Sub-item detail</td></tr>
            </table>";

        var result = HtmlToTextConverter.Convert(html);

        Assert.Contains("Sub-item detail", result);
    }

    [Fact]
    public void Convert_ListItems_AddsBullets()
    {
        var html = "<ul><li>First</li><li>Second</li></ul>";
        var result = HtmlToTextConverter.Convert(html);

        Assert.Contains("• First", result);
        Assert.Contains("• Second", result);
    }

    [Fact]
    public void Convert_SectionClass_AddsBlankLine()
    {
        var html = @"
            <p>First paragraph</p>
            <div class=""section"">
                <p>Section content</p>
            </div>";

        var result = HtmlToTextConverter.Convert(html);
        var lines = result.Split('\n');

        // Should have a blank line before section content
        var sectionIndex = Array.FindIndex(lines, l => l.Contains("Section content"));
        Assert.True(sectionIndex > 0);
        Assert.Equal("", lines[sectionIndex - 1]);
    }

    [Fact]
    public void Convert_RemovesScriptAndStyle()
    {
        var html = @"
            <script>alert('test');</script>
            <style>.hidden { display: none; }</style>
            <p>Visible content</p>";

        var result = HtmlToTextConverter.Convert(html);

        Assert.DoesNotContain("alert", result);
        Assert.DoesNotContain("display", result);
        Assert.Contains("Visible content", result);
    }

    [Fact]
    public void Convert_DecodesHtmlEntities()
    {
        var html = "<p>Terms &amp; Conditions</p>";
        var result = HtmlToTextConverter.Convert(html);
        Assert.Contains("Terms & Conditions", result);
    }

}
