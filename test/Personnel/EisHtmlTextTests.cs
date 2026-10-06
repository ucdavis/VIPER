using Viper.Areas.Personnel.Models.Eis;

namespace Viper.test.Personnel;

/// <summary>
/// MyInfoVault stores most entries as HTML; EisHtmlText turns them into plain text so the page
/// never renders markup it didn't write.
/// </summary>
public sealed class EisHtmlTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p> </p>")]
    public void ToText_BlankHtml_IsNull(string? html)
    {
        Assert.Null(EisHtmlText.ToText(html));
    }

    [Fact]
    public void ToText_StripsTagsDecodesEntitiesAndCollapsesWhitespace()
    {
        Assert.Equal(
            "AVMA & ACVIM <member>",
            EisHtmlText.ToText("<p><b>AVMA</b>\r\n  &amp; ACVIM &lt;member&gt;</p>"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("<p></p>")]
    public void ToItems_BlankHtml_IsEmpty(string? html)
    {
        Assert.Empty(EisHtmlText.ToItems(html));
    }

    [Fact]
    public void ToItems_SplitsParagraphsAndListItems()
    {
        Assert.Equal(
            ["Infectious disease", "Equine medicine", "Cardiology"],
            EisHtmlText.ToItems("<p>Infectious disease</p><p> </p><p>Equine <b>medicine</b></p><ul><li>Cardiology</li></ul>"));
    }

    [Fact]
    public void ToItems_KeepsNestedItemsWithTheirParent()
    {
        Assert.Equal(["Oncology Radiation"], EisHtmlText.ToItems("<li>Oncology <p>Radiation</p></li>"));
    }

    [Fact]
    public void ToItems_TextWithoutParagraphs_IsOneItem()
    {
        Assert.Equal(["Small animal surgery"], EisHtmlText.ToItems("Small animal <em>surgery</em>"));
    }
}
