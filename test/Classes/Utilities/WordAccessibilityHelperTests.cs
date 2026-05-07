using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Viper.Classes.Utilities;
using DrawingNonVisualPicture = DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties;
using DrawingDocProperties = DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties;

namespace Viper.test.Classes.Utilities;

public class WordAccessibilityHelperTests
{
#pragma warning disable S3220 // OpenXML SDK uses params overloads by design for fluent object construction
    private static (WordprocessingDocument doc, MainDocumentPart mainPart) NewDocument(MemoryStream stream)
    {
        var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        return (doc, mainPart);
    }
#pragma warning restore S3220

    private static Style? FindStyle(Styles styles, string styleId) =>
        styles.Elements<Style>().FirstOrDefault(s => s.StyleId?.Value == styleId);

    [Fact]
    public void EnsureAccessibilityStyles_RegistersHeading1WithOutlineLevel0()
    {
        using var stream = new MemoryStream();
        var (doc, mainPart) = NewDocument(stream);

        WordAccessibilityHelper.EnsureAccessibilityStyles(mainPart);

        var styles = mainPart.StyleDefinitionsPart!.Styles!;
        var h1 = FindStyle(styles, WordAccessibilityHelper.Heading1StyleId);
        Assert.NotNull(h1);
        var outline = h1.StyleParagraphProperties?.Elements<OutlineLevel>().FirstOrDefault();
        Assert.NotNull(outline);
        Assert.Equal(0, outline.Val?.Value);

        doc.Dispose();
    }

    [Fact]
    public void EnsureAccessibilityStyles_TitleStyleIsNotMarkedDefault()
    {
        // Regression guard: Default = true on the Title paragraph style is wrong —
        // only the Normal style should be default for its type.
        using var stream = new MemoryStream();
        var (doc, mainPart) = NewDocument(stream);

        WordAccessibilityHelper.EnsureAccessibilityStyles(mainPart);

        var styles = mainPart.StyleDefinitionsPart!.Styles!;
        var title = FindStyle(styles, WordAccessibilityHelper.TitleStyleId);
        Assert.NotNull(title);
        Assert.False(title.Default?.Value ?? false);

        doc.Dispose();
    }

    [Fact]
    public void EnsureAccessibilityStyles_TitleAndHeading1HaveDifferentFontSizes()
    {
        // Regression guard: visually distinguishable so users can tell the document
        // title apart from a top-level heading.
        using var stream = new MemoryStream();
        var (doc, mainPart) = NewDocument(stream);

        WordAccessibilityHelper.EnsureAccessibilityStyles(mainPart);

        var styles = mainPart.StyleDefinitionsPart!.Styles!;
        var titleSize = FindStyle(styles, WordAccessibilityHelper.TitleStyleId)
            ?.StyleRunProperties?.Elements<FontSize>().FirstOrDefault()?.Val?.Value;
        var h1Size = FindStyle(styles, WordAccessibilityHelper.Heading1StyleId)
            ?.StyleRunProperties?.Elements<FontSize>().FirstOrDefault()?.Val?.Value;

        Assert.NotNull(titleSize);
        Assert.NotNull(h1Size);
        Assert.NotEqual(titleSize, h1Size);

        doc.Dispose();
    }

    [Fact]
    public void EnsureAccessibilityStyles_SetsDefaultDocumentLanguage()
    {
        using var stream = new MemoryStream();
        var (doc, mainPart) = NewDocument(stream);

        WordAccessibilityHelper.EnsureAccessibilityStyles(mainPart, language: "fr-FR");

        var docDefaults = mainPart.StyleDefinitionsPart!.Styles!.Elements<DocDefaults>().Single();
        var lang = docDefaults.RunPropertiesDefault?.RunPropertiesBaseStyle?
            .Elements<Languages>().FirstOrDefault()?.Val?.Value;
        Assert.Equal("fr-FR", lang);

        doc.Dispose();
    }

    [Fact]
    public void EnsureAccessibilityStyles_IsIdempotent()
    {
        using var stream = new MemoryStream();
        var (doc, mainPart) = NewDocument(stream);

        WordAccessibilityHelper.EnsureAccessibilityStyles(mainPart);
        WordAccessibilityHelper.EnsureAccessibilityStyles(mainPart);

        var styles = mainPart.StyleDefinitionsPart!.Styles!;
        var styleIds = new[]
        {
            WordAccessibilityHelper.TitleStyleId,
            WordAccessibilityHelper.Heading1StyleId,
            WordAccessibilityHelper.Heading2StyleId,
            WordAccessibilityHelper.Heading3StyleId,
        };
        foreach (var id in styleIds)
        {
            Assert.Equal(1, styles.Elements<Style>().Count(s => s.StyleId?.Value == id));
        }

        doc.Dispose();
    }

    [Fact]
    public void SetImageAltText_OnlyDescription_LeavesTitleNull()
    {
        // Regression guard: setting the same string for Title + Description
        // raises an Office Accessibility Checker warning. Title must stay null
        // unless caller explicitly opts in.
        var wpDocProps = new DrawingDocProperties { Id = 1U, Name = "Img1" };
        var picProps = new DrawingNonVisualPicture { Id = 1U, Name = "Img1.jpg" };

        WordAccessibilityHelper.SetImageAltText(wpDocProps, picProps, "Photo of student");

        Assert.Equal("Photo of student", wpDocProps.Description?.Value);
        Assert.Equal("Photo of student", picProps.Description?.Value);
        Assert.Null(wpDocProps.Title?.Value);
        Assert.Null(picProps.Title?.Value);
    }

    [Fact]
    public void SetImageAltText_WithExplicitTitle_SetsBoth()
    {
        var wpDocProps = new DrawingDocProperties { Id = 1U, Name = "Img1" };
        var picProps = new DrawingNonVisualPicture { Id = 1U, Name = "Img1.jpg" };

        WordAccessibilityHelper.SetImageAltText(wpDocProps, picProps,
            description: "Detailed long description for the chart",
            title: "Q3 revenue");

        Assert.Equal("Q3 revenue", wpDocProps.Title?.Value);
        Assert.Equal("Q3 revenue", picProps.Title?.Value);
        Assert.Equal("Detailed long description for the chart", wpDocProps.Description?.Value);
    }

    [Fact]
    public void CreateHeadingParagraph_AppliesGivenStyleId()
    {
        var paragraph = WordAccessibilityHelper.CreateHeadingParagraph(
            "Section 1", WordAccessibilityHelper.Heading2StyleId);

        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        Assert.Equal(WordAccessibilityHelper.Heading2StyleId, styleId);
        Assert.Equal("Section 1", paragraph.InnerText);
    }

    [Fact]
    public void MarkAsTableHeaderRow_AddsTblHeaderProperty()
    {
        var row = new TableRow();

        WordAccessibilityHelper.MarkAsTableHeaderRow(row);

        var props = row.Elements<TableRowProperties>().Single();
        Assert.Single(props.Elements<TableHeader>());
    }

    [Fact]
    public void MarkAsTableHeaderRow_IsIdempotent()
    {
        var row = new TableRow();

        WordAccessibilityHelper.MarkAsTableHeaderRow(row);
        WordAccessibilityHelper.MarkAsTableHeaderRow(row);

        var props = row.Elements<TableRowProperties>().Single();
        Assert.Single(props.Elements<TableHeader>());
    }

    [Fact]
    public void SetCoreProperties_PopulatesTitleSubjectCreatorLanguage()
    {
        using var stream = new MemoryStream();
        var (doc, _) = NewDocument(stream);

        WordAccessibilityHelper.SetCoreProperties(doc,
            title: "Annual Report",
            subject: "FY26 figures",
            creator: "Test Suite",
            language: "en-GB");

        Assert.Equal("Annual Report", doc.PackageProperties.Title);
        Assert.Equal("FY26 figures", doc.PackageProperties.Subject);
        Assert.Equal("Test Suite", doc.PackageProperties.Creator);
        Assert.Equal("en-GB", doc.PackageProperties.Language);

        doc.Dispose();
    }

    [Fact]
    public void SetCoreProperties_FallsBackSubjectToTitleWhenNull()
    {
        using var stream = new MemoryStream();
        var (doc, _) = NewDocument(stream);

        WordAccessibilityHelper.SetCoreProperties(doc, title: "Just A Title");

        Assert.Equal("Just A Title", doc.PackageProperties.Subject);

        doc.Dispose();
    }
}
