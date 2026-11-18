using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes.Utilities;

namespace Viper.Areas.ReportBuilder.Exporters;

/// <summary>
/// Writes a report to a Letter PDF tagged for PDF/UA: a heading (title, when it was generated,
/// filters, confidentiality notice, truncation note), then one table whose header repeats on
/// every page. The notice and page numbers are in the footer.
/// </summary>
public static class ReportPdfWriter
{
    public const string ContentType = "application/pdf";
    public const string Extension = ".pdf";

    private const float FontSize = 8f;
    private const float TitleFontSize = 13f;
    private const float FooterFontSize = 7f;
    private const float SectionSpacing = 8f;
    private const float CellPaddingVertical = 2f;
    private const float CellPaddingHorizontal = 3f;
    private const float RuleWidth = 0.5f;
    private const string HeaderFill = "#E8E8E8";
    private const string RuleColor = "#CCCCCC";

    public static byte[] Write(ReportExportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Document.Create(container => container.Page(page =>
            {
                page.Size(document.Landscape ? PageSizes.Letter.Landscape() : PageSizes.Letter);
                page.MarginHorizontal(0.5f, Unit.Inch);
                page.MarginVertical(0.4f, Unit.Inch);
                page.DefaultTextStyle(style => style.FontSize(FontSize));
                page.Header().Element(header => ComposeHeading(header, document));
                page.Content().PaddingTop(SectionSpacing).Element(content => ComposeTable(content, document));
                page.Footer().Element(footer => ComposeFooter(footer, document.SensitiveNotice));
            }))
            .WithAccessibility(document.Title)
            .GeneratePdf();
    }

    private static void ComposeHeading(IContainer container, ReportExportDocument document)
    {
        container.Column(column =>
        {
            column.Item().SemanticHeader1().Text(document.Title).SemiBold().FontSize(TitleFontSize);
            column.Item().Text(ReportExportText.Generated(document.GeneratedAt));
            if (ReportExportText.Filters(document.Filters) is { } filters)
            {
                column.Item().Text(filters);
            }

            if (document.SensitiveNotice is { } notice)
            {
                column.Item().Text(notice).Italic();
            }

            if (document.Truncated)
            {
                column.Item().Text(ReportExportText.Truncated());
            }
        });
    }

    private static void ComposeTable(IContainer container, ReportExportDocument document)
    {
        container.SemanticTable().Table(table =>
        {
            table.ColumnsDefinition(definition =>
            {
                for (int index = 0; index < document.Columns.Count; index++)
                {
                    definition.RelativeColumn();
                }
            });

            table.Header(header =>
            {
                foreach (ReportResultColumn column in document.Columns)
                {
                    Align(Padded(header.Cell().Background(HeaderFill)), column.Format)
                        .Text(column.Label)
                        .SemiBold();
                }
            });

            foreach (IReadOnlyDictionary<string, object?> row in document.Rows)
            {
                foreach (ReportResultColumn column in document.Columns)
                {
                    // An empty cell would drop out of the PDF's structure tree, so it shows a dash.
                    string text = PdfAccessibilityHelper.OrDash(ReportCellFormatter.Text(row.GetValueOrDefault(column.Name), column.Format));
                    Align(Padded(table.Cell().BorderBottom(RuleWidth).BorderColor(RuleColor)), column.Format).Text(text);
                }
            }
        });
    }

    private static void ComposeFooter(IContainer container, string? notice)
    {
        container.Column(column =>
        {
            if (notice is not null)
            {
                column.Item().AlignCenter().Text(notice).FontSize(FooterFontSize).Italic();
            }

            column.Item().PageNumberFooter();
        });
    }

    private static IContainer Padded(IContainer cell)
    {
        return cell.PaddingVertical(CellPaddingVertical).PaddingHorizontal(CellPaddingHorizontal);
    }

    private static IContainer Align(IContainer cell, ReportColumnFormat format)
    {
        return ReportCellFormatter.IsNumeric(format) ? cell.AlignRight() : cell.AlignLeft();
    }
}
