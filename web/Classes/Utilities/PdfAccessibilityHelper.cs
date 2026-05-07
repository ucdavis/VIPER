using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Viper.Classes.Utilities;

/// <summary>
/// QuestPDF accessibility defaults. Centralizes PDF/UA conformance and document
/// metadata so every report PDF gets consistent tagging and core properties.
/// The QuestPDF Community licence is configured once at application startup
/// (see <c>Program.cs</c>); this helper does not touch global settings.
/// Pair with <see cref="QuestPDF.Fluent.SemanticExtensions"/> calls inside the
/// document body (SemanticHeader1, SemanticImage, SemanticIgnore, etc.) to
/// satisfy WCAG 2.1 SC 1.3.1 / PDF-UA-1 expectations.
/// </summary>
public static class PdfAccessibilityHelper
{
    public const string DefaultAuthor = "UC Davis School of Veterinary Medicine";
    public const string DefaultLanguage = "en-US";

    /// <summary>
    /// Apply document metadata and PDF/UA conformance to a QuestPDF document
    /// in one call.
    /// </summary>
    /// <param name="document">Result of <c>Document.Create(...)</c>.</param>
    /// <param name="title">Document title — surfaces in the PDF reader title bar and is required for PDF/UA.</param>
    /// <param name="subject">Optional subject / brief description.</param>
    /// <param name="keywords">Optional comma-separated keyword list.</param>
    /// <param name="author">Defaults to <see cref="DefaultAuthor"/>.</param>
    /// <param name="language">BCP-47 / ISO 639 language tag. Defaults to <see cref="DefaultLanguage"/>.</param>
    public static Document WithAccessibility(
        this Document document,
        string title,
        string? subject = null,
        string? keywords = null,
        string author = DefaultAuthor,
        string language = DefaultLanguage)
    {
        var now = DateTimeOffset.Now;

        return document
            .WithMetadata(new DocumentMetadata
            {
                Title = title,
                Author = author,
                Subject = subject ?? title,
                Keywords = keywords ?? string.Empty,
                Language = language,
                CreationDate = now,
                ModifiedDate = now
            })
            .WithSettings(new DocumentSettings
            {
                PDFUA_Conformance = PDFUA_Conformance.PDFUA_1
            });
    }
}
