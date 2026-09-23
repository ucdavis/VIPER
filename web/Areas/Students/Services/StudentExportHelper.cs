using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Classes.Utilities;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Layout shared by the student self-service app exports (emergency contacts, career selection):
/// a "Generated" timestamp under the title, and single-table landscape PDFs.
/// </summary>
public static class StudentExportHelper
{
    private static string BuildGeneratedLabel(DateTime generatedAt) =>
        $"Generated {generatedAt:M/d/yyyy h:mm tt}";

    /// <summary>
    /// Add the report's worksheet, with the generated timestamp in row 1 and bold column headers
    /// in row 2. Data rows start at row 3.
    /// </summary>
    public static IXLWorksheet AddReportWorksheet(XLWorkbook workbook, string title, string subject, IReadOnlyList<string> headers)
    {
        ExcelAccessibilityHelper.SetCoreProperties(workbook, title, subject: subject);

        var ws = workbook.Worksheets.Add(title);

        ws.Cell(1, 1).Value = BuildGeneratedLabel(DateTime.Now);
        ws.Cell(1, 1).Style.Font.Italic = true;

        for (int col = 0; col < headers.Count; col++)
        {
            ws.Cell(2, col + 1).Value = headers[col];
            ws.Cell(2, col + 1).Style.Font.Bold = true;
        }

        return ws;
    }

    /// <summary>
    /// Generate an accessible A4 landscape PDF holding one table under a title and generated
    /// timestamp, with page numbers in the footer.
    /// </summary>
    /// <param name="title">Heading printed on every page, and the document title recorded in the
    /// PDF's accessibility metadata.</param>
    /// <param name="subject">The document subject recorded in the PDF's accessibility metadata.
    /// It is not drawn on the page.</param>
    /// <param name="buildTable">Defines the columns, header row and body rows of the table.</param>
    public static byte[] GenerateTablePdf(string title, string subject, Action<TableDescriptor> buildTable)
    {
        // Capture once so per-page header delegates don't drift across pages.
        var generatedLabel = BuildGeneratedLabel(DateTime.Now);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0.5f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().SemanticHeader1().Text(title)
                        .SemiBold().FontSize(14).AlignCenter();
                    col.Item().Text(generatedLabel)
                        .FontSize(8).Italic().AlignCenter();
                });

                page.Content().SemanticTable().Table(buildTable);

                page.Footer().PageNumberFooter();
            });
        })
        .WithAccessibility(title, subject: subject)
        .GeneratePdf();
    }
}
