using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Students.Models;
using Viper.Classes.Utilities;
using static Viper.Classes.Utilities.PdfAccessibilityHelper;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Exports for the student career selection app. Each report has a
/// completeness-summary "overview" variant and a full-detail variant.
/// </summary>
public interface ICareerSelectionExportService
{
    /// <summary>
    /// Generate the overview Excel workbook — one row per student with a
    /// completeness flag for each career selection field.
    /// </summary>
    MemoryStream GenerateOverviewExcel(List<StudentCareerListItemDto> data);

    /// <summary>
    /// Generate the overview PDF — one row per student with a completeness
    /// flag for each career selection field.
    /// </summary>
    byte[] GenerateOverviewPdf(List<StudentCareerListItemDto> data);

    /// <summary>
    /// Generate the overview CSV: the same rows as the overview workbook, for a reader who wants
    /// the data rather than the formatting.
    /// </summary>
    byte[] GenerateOverviewCsv(List<StudentCareerListItemDto> data);

    /// <summary>
    /// Generate the full-detail Excel workbook — one row per student with the
    /// selected value for each career selection field.
    /// </summary>
    MemoryStream GenerateExcel(List<StudentCareerReportDto> data);

    /// <summary>
    /// Generate the full-detail PDF — one row per student with the selected
    /// value for each career selection field.
    /// </summary>
    byte[] GeneratePdf(List<StudentCareerReportDto> data);

    /// <summary>
    /// Generate the full-detail CSV: the same rows as the report workbook, whole statements
    /// included.
    /// </summary>
    byte[] GenerateCsv(List<StudentCareerReportDto> data);
}

public class CareerSelectionExportService : ICareerSelectionExportService
{
    /// <summary>
    /// Both reports carry the same columns in the same order as the grids they mirror; the
    /// overview fills them with completeness flags and the report with the selected values.
    /// </summary>
    private static readonly string[] ColumnHeaders =
    [
        "Class", "Name", "Email", "Career", "Species 1", "Species 2",
        "Post Grad", "Mentor", "Short Term", "Long Term", "Last Updated"
    ];

    /// <summary>
    /// Career statements run to 5000 characters, which is unreadable in a landscape table
    /// and can push a single row past a whole page, so the PDF shows an opening excerpt.
    /// </summary>
    private const int PdfStatementMaxLength = 200;

    private const int ShortTermColumn = 9;
    private const int LongTermColumn = 10;

    /// <summary>Statement columns sized to their contents would swallow the sheet.</summary>
    private const double StatementColumnWidth = 60;

    public MemoryStream GenerateOverviewExcel(List<StudentCareerListItemDto> data)
    {
        using var wb = new XLWorkbook();
        var ws = StudentExportHelper.AddReportWorksheet(wb, "Career Selection Overview",
            "Student career selection overview", ColumnHeaders);

        for (int i = 0; i < data.Count; i++)
        {
            var row = i + 3;
            var d = data[i];

            ws.Cell(row, 1).Value = ExcelHelper.SanitizeStringCell(d.ClassLevel);
            ws.Cell(row, 2).Value = ExcelHelper.SanitizeStringCell(d.FullName);
            ws.Cell(row, 3).Value = ExcelHelper.SanitizeStringCell(d.Email);
            ws.Cell(row, 4).Value = CompletenessLabel(d.DirectionCompleted);
            ws.Cell(row, 5).Value = CompletenessLabel(d.PrimaryFocusCompleted);
            ws.Cell(row, 6).Value = OptionalCompletenessLabel(d.SecondaryFocusCompleted);
            ws.Cell(row, 7).Value = CompletenessLabel(d.PostGradCompleted);
            ws.Cell(row, 8).Value = ExcelHelper.SanitizeStringCell(d.MentorName ?? string.Empty);
            ws.Cell(row, ShortTermColumn).Value = CompletenessLabel(d.ShortTermPlansCompleted);
            ws.Cell(row, LongTermColumn).Value = CompletenessLabel(d.LongTermPlansCompleted);
            ws.Cell(row, 11).Value = d.LastUpdated?.ToString("M/d/yyyy") ?? "";
        }

        if (data.Count > 0)
        {
            ExcelAccessibilityHelper.PromoteToAccessibleTable(
                ws.Range(2, 1, data.Count + 2, ColumnHeaders.Length),
                "CareerSelectionOverview");
        }

        ws.Columns().AdjustToContents();

        return ExcelHelper.SaveToStream(wb);
    }

    public byte[] GenerateOverviewPdf(List<StudentCareerListItemDto> data) =>
        StudentExportHelper.GenerateTablePdf("Career Selection Overview", "Student career selection overview", table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();     // Class
                columns.RelativeColumn(2.5f); // Name
                columns.RelativeColumn(2.5f); // Email
                columns.RelativeColumn(1.2f); // Career
                columns.RelativeColumn(1.2f); // Species 1
                columns.RelativeColumn(1.2f); // Species 2
                columns.RelativeColumn(1.2f); // Post Grad
                columns.RelativeColumn(2);    // Mentor
                columns.RelativeColumn(1.2f); // Short Term
                columns.RelativeColumn(1.2f); // Long Term
                columns.RelativeColumn(1.3f); // Last Updated
            });

            PdfHeaderRow(table);
            PdfEmptyRowIfNone(table, data.Count);

            foreach (var d in data)
            {
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.ClassLevel));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.FullName));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.Email));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.DirectionCompleted));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.PrimaryFocusCompleted));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(OptionalCompletenessLabel(d.SecondaryFocusCompleted)));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.PostGradCompleted));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.MentorName));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.ShortTermPlansCompleted));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.LongTermPlansCompleted));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(d.LastUpdated?.ToString("M/d/yyyy") ?? "—");
            }
        });

    public byte[] GenerateOverviewCsv(List<StudentCareerListItemDto> data) =>
        CsvExportHelper.Build(ColumnHeaders, data.Select(d => new[]
        {
            d.ClassLevel,
            d.FullName,
            d.Email,
            CompletenessLabel(d.DirectionCompleted),
            CompletenessLabel(d.PrimaryFocusCompleted),
            OptionalCompletenessLabel(d.SecondaryFocusCompleted),
            CompletenessLabel(d.PostGradCompleted),
            d.MentorName ?? string.Empty,
            CompletenessLabel(d.ShortTermPlansCompleted),
            CompletenessLabel(d.LongTermPlansCompleted),
            d.LastUpdated?.ToString("M/d/yyyy") ?? string.Empty,
        }));

    public MemoryStream GenerateExcel(List<StudentCareerReportDto> data)
    {
        using var wb = new XLWorkbook();
        var ws = StudentExportHelper.AddReportWorksheet(wb, "Career Selection Report",
            "Student career selection report", ColumnHeaders);

        for (int i = 0; i < data.Count; i++)
        {
            var row = i + 3;
            var d = data[i];

            ws.Cell(row, 1).Value = ExcelHelper.SanitizeStringCell(d.ClassLevel);
            ws.Cell(row, 2).Value = ExcelHelper.SanitizeStringCell(d.FullName);
            ws.Cell(row, 3).Value = ExcelHelper.SanitizeStringCell(d.Email);
            ws.Cell(row, 4).Value = ExcelHelper.SanitizeStringCell(d.Direction);
            ws.Cell(row, 5).Value = ExcelHelper.SanitizeStringCell(d.PrimaryFocus);
            ws.Cell(row, 6).Value = ExcelHelper.SanitizeStringCell(d.SecondaryFocus);
            ws.Cell(row, 7).Value = ExcelHelper.SanitizeStringCell(d.PostGrad);
            ws.Cell(row, 8).Value = ExcelHelper.SanitizeStringCell(d.MentorName);
            ws.Cell(row, ShortTermColumn).Value = ExcelHelper.SanitizeStringCell(d.ShortTermPlans);
            ws.Cell(row, ShortTermColumn).Style.Alignment.WrapText = true;
            ws.Cell(row, LongTermColumn).Value = ExcelHelper.SanitizeStringCell(d.LongTermPlans);
            ws.Cell(row, LongTermColumn).Style.Alignment.WrapText = true;
            ws.Cell(row, 11).Value = d.LastUpdated?.ToString("M/d/yyyy") ?? "";
        }

        if (data.Count > 0)
        {
            ExcelAccessibilityHelper.PromoteToAccessibleTable(
                ws.Range(2, 1, data.Count + 2, ColumnHeaders.Length),
                "CareerSelectionReport");
        }

        ws.Columns().AdjustToContents();
        ws.Column(ShortTermColumn).Width = StatementColumnWidth;
        ws.Column(LongTermColumn).Width = StatementColumnWidth;

        return ExcelHelper.SaveToStream(wb);
    }

    public byte[] GeneratePdf(List<StudentCareerReportDto> data) =>
        StudentExportHelper.GenerateTablePdf("Career Selection Report", "Student career selection report", table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(0.8f); // Class
                columns.RelativeColumn(2);    // Name
                columns.RelativeColumn(2.2f); // Email
                columns.RelativeColumn(1.5f); // Career
                columns.RelativeColumn(1.5f); // Species 1
                columns.RelativeColumn(1.5f); // Species 2
                columns.RelativeColumn(1.5f); // Post Grad
                columns.RelativeColumn(1.8f); // Mentor
                columns.RelativeColumn(3.5f); // Short Term
                columns.RelativeColumn(3.5f); // Long Term
                columns.RelativeColumn(1.2f); // Last Updated
            });

            PdfHeaderRow(table);
            PdfEmptyRowIfNone(table, data.Count);

            foreach (var d in data)
            {
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.ClassLevel));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.FullName));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.Email));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.Direction));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.PrimaryFocus));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.SecondaryFocus));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.PostGrad));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.MentorName));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(Excerpt(d.ShortTermPlans));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(Excerpt(d.LongTermPlans));
                table.Cell().BorderBottom(0.5f).Padding(2).Text(d.LastUpdated?.ToString("M/d/yyyy") ?? "—");
            }
        });

    public byte[] GenerateCsv(List<StudentCareerReportDto> data) =>
        CsvExportHelper.Build(ColumnHeaders, data.Select(d => new[]
        {
            d.ClassLevel,
            d.FullName,
            d.Email,
            d.Direction,
            d.PrimaryFocus,
            d.SecondaryFocus,
            d.PostGrad,
            d.MentorName,
            d.ShortTermPlans,
            d.LongTermPlans,
            d.LastUpdated?.ToString("M/d/yyyy") ?? string.Empty,
        }));

    private static void PdfHeaderRow(TableDescriptor table)
    {
        var hdrStyle = TextStyle.Default.FontSize(8).SemiBold();
        table.Header(header =>
        {
            foreach (var label in ColumnHeaders)
            {
                header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2)
                    .Text(label).Style(hdrStyle);
            }
        });
    }

    /// <summary>
    /// The grid's own wording for a search that matches nothing, so an empty PDF reads as
    /// "nothing matched" rather than as a failed render.
    /// </summary>
    private const string NoRecordsMessage = "No matching records found";

    private static void PdfEmptyRowIfNone(TableDescriptor table, int rowCount)
    {
        if (rowCount == 0)
        {
            table.Cell().ColumnSpan((uint)ColumnHeaders.Length).Padding(2).Text(NoRecordsMessage).Italic();
        }
    }

    private static string CompletenessLabel(bool complete) => complete ? "Yes" : "No";

    /// <summary>
    /// A second species focus is optional, so the grid shows no icon at all when it is
    /// unset rather than flagging it as missing. The form still prompts for it; the difference
    /// is intentional.
    /// </summary>
    private static string OptionalCompletenessLabel(bool complete) => complete ? "Yes" : "";

    /// <summary>
    /// Trims a career statement to an opening excerpt for the PDF, marking text that was cut
    /// so a reader knows to go to the Excel export or the app for the rest.
    /// </summary>
    private static string Excerpt(string? value)
    {
        var text = OrDash(value);
        return text.Length <= PdfStatementMaxLength
            ? text
            : text[..(PdfStatementMaxLength - 1)].TrimEnd() + "…";
    }
}
