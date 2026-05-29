using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Viper.Classes.Utilities;

namespace Viper.test.Classes.Utilities;

public class PdfAccessibilityHelperTests
{
    private static Document MinimalDocument() =>
        Document.Create(container =>
            container.Page(page => page.Content().Text("placeholder")));

    [Fact]
    public void WithAccessibility_PopulatesAllMetadataFields()
    {
        var document = MinimalDocument().WithAccessibility(
            title: "Annual Report",
            subject: "FY26 figures",
            keywords: "merit, evaluation",
            author: "Test Suite",
            language: "en-GB");

        var meta = document.GetMetadata();
        Assert.Equal("Annual Report", meta.Title);
        Assert.Equal("FY26 figures", meta.Subject);
        Assert.Equal("merit, evaluation", meta.Keywords);
        Assert.Equal("Test Suite", meta.Author);
        Assert.Equal("en-GB", meta.Language);
    }

    [Fact]
    public void WithAccessibility_DefaultsAuthorToUcDavisSvm()
    {
        var document = MinimalDocument().WithAccessibility(title: "Doc");

        Assert.Equal(PdfAccessibilityHelper.DefaultAuthor, document.GetMetadata().Author);
    }

    [Fact]
    public void WithAccessibility_DefaultsLanguageToEnUs()
    {
        var document = MinimalDocument().WithAccessibility(title: "Doc");

        Assert.Equal(PdfAccessibilityHelper.DefaultLanguage, document.GetMetadata().Language);
    }

    [Fact]
    public void WithAccessibility_SubjectFallsBackToTitleWhenNull()
    {
        var document = MinimalDocument().WithAccessibility(title: "Title Only");

        Assert.Equal("Title Only", document.GetMetadata().Subject);
    }

    [Fact]
    public void WithAccessibility_KeywordsDefaultToEmptyStringWhenNull()
    {
        var document = MinimalDocument().WithAccessibility(title: "Doc");

        Assert.Equal(string.Empty, document.GetMetadata().Keywords);
    }

    [Fact]
    public void WithAccessibility_EnablesPdfUa1Conformance()
    {
        var document = MinimalDocument().WithAccessibility(title: "Doc");

        Assert.Equal(PDFUA_Conformance.PDFUA_1, document.GetSettings().PDFUA_Conformance);
    }

    [Fact]
    public void WithAccessibility_StampsCreationAndModifiedDates()
    {
        var before = DateTimeOffset.Now.AddSeconds(-5);
        var document = MinimalDocument().WithAccessibility(title: "Doc");
        var after = DateTimeOffset.Now.AddSeconds(5);

        var meta = document.GetMetadata();
        Assert.InRange(meta.CreationDate, before, after);
        Assert.InRange(meta.ModifiedDate, before, after);
    }
}
