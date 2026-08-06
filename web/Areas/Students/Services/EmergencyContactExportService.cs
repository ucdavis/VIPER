using ClosedXML.Excel;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Students.Models;
using Viper.Classes.Utilities;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Exports for the student emergency-contact app. Each report has a
/// completeness-summary "overview" variant and a full-detail variant.
/// </summary>
public interface IEmergencyContactExportService
{
    /// <summary>
    /// Generate the overview Excel workbook — one row per student with
    /// completeness counts for each contact category.
    /// </summary>
    MemoryStream GenerateOverviewExcel(List<StudentContactListItemDto> data);

    /// <summary>
    /// Generate the overview PDF — one row per student with completeness
    /// counts for each contact category.
    /// </summary>
    byte[] GenerateOverviewPdf(List<StudentContactListItemDto> data);

    /// <summary>
    /// Generate the full-detail Excel workbook — one row per student with
    /// the formatted blocks for each contact category.
    /// </summary>
    MemoryStream GenerateExcel(List<StudentContactReportDto> data);

    /// <summary>
    /// Generate the full-detail PDF — one row per student with formatted
    /// blocks for each contact category.
    /// </summary>
    byte[] GeneratePdf(List<StudentContactReportDto> data);
}

public class EmergencyContactExportService : IEmergencyContactExportService
{
    private static string BuildGeneratedLabel(DateTime generatedAt) =>
        $"Generated {generatedAt:M/d/yyyy h:mm tt}";

    public MemoryStream GenerateOverviewExcel(List<StudentContactListItemDto> data)
    {
        using var wb = new XLWorkbook();
        const string title = "Emergency Contact Overview";
        ExcelAccessibilityHelper.SetCoreProperties(wb, title,
            subject: "Student emergency contact overview");

        var ws = wb.Worksheets.Add(title);

        ws.Cell(1, 1).Value = BuildGeneratedLabel(DateTime.Now);
        ws.Cell(1, 1).Style.Font.Italic = true;

        var headers = new[]
        {
            "Name", "Class Level", "Email", "Phone",
            "Student Info", "Local Contact", "Emergency Contact", "Permanent Contact",
            "Last Updated"
        };

        for (int col = 0; col < headers.Length; col++)
        {
            ws.Cell(2, col + 1).Value = headers[col];
            ws.Cell(2, col + 1).Style.Font.Bold = true;
        }

        for (int i = 0; i < data.Count; i++)
        {
            var row = i + 3;
            var d = data[i];

            ws.Cell(row, 1).Value = ExcelHelper.SanitizeStringCell(d.FullName);
            ws.Cell(row, 2).Value = ExcelHelper.SanitizeStringCell(d.ClassLevel);
            ws.Cell(row, 3).Value = ExcelHelper.SanitizeStringCell(d.Email);
            ws.Cell(row, 4).Value = ExcelHelper.SanitizeStringCell(d.CellPhone);
            ws.Cell(row, 5).Value = CompletenessLabel(d.StudentInfoComplete, d.StudentInfoTotal);
            ws.Cell(row, 6).Value = CompletenessLabel(d.LocalContactComplete, d.LocalContactTotal);
            ws.Cell(row, 7).Value = CompletenessLabel(d.EmergencyContactComplete, d.EmergencyContactTotal);
            ws.Cell(row, 8).Value = CompletenessLabel(d.PermanentContactComplete, d.PermanentContactTotal);
            ws.Cell(row, 9).Value = d.LastUpdated?.ToString("M/d/yyyy") ?? "";
        }

        if (data.Count > 0)
        {
            ExcelAccessibilityHelper.PromoteToAccessibleTable(
                ws.Range(2, 1, data.Count + 2, headers.Length),
                "EmergencyContactOverview");
        }

        ws.Columns().AdjustToContents();

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    public byte[] GenerateOverviewPdf(List<StudentContactListItemDto> data)
    {
        // Capture once so per-page header delegates don't drift across pages.
        var generatedLabel = BuildGeneratedLabel(DateTime.Now);
        const string title = "Emergency Contact Overview";

        var document = Document.Create(container =>
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

                page.Content().SemanticTable().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2.5f); // Name
                        columns.RelativeColumn();    // Class
                        columns.RelativeColumn(2.5f); // Email
                        columns.RelativeColumn(1.5f); // Phone
                        columns.RelativeColumn();    // Student Info
                        columns.RelativeColumn();    // Local
                        columns.RelativeColumn();    // Emergency
                        columns.RelativeColumn();    // Permanent
                        columns.RelativeColumn(1.2f); // Last Updated
                    });

                    var hdrStyle = TextStyle.Default.FontSize(8).SemiBold();
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Name").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Class").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Email").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Phone").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Student Info").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Local").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Emergency").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Permanent").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Updated").Style(hdrStyle);
                    });

                    foreach (var d in data)
                    {
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.FullName));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.ClassLevel));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.Email));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.CellPhone));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.StudentInfoComplete, d.StudentInfoTotal));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.LocalContactComplete, d.LocalContactTotal));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.EmergencyContactComplete, d.EmergencyContactTotal));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(CompletenessLabel(d.PermanentContactComplete, d.PermanentContactTotal));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(d.LastUpdated?.ToString("M/d/yyyy") ?? "—");
                    }
                });

                page.Footer().SemanticIgnore().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        })
        .WithAccessibility(title, subject: "Student emergency contact overview");

        return document.GeneratePdf();
    }

    public MemoryStream GenerateExcel(List<StudentContactReportDto> data)
    {
        using var wb = new XLWorkbook();
        const string title = "Emergency Contact Report";
        ExcelAccessibilityHelper.SetCoreProperties(wb, title,
            subject: "Student emergency contact report");

        var ws = wb.Worksheets.Add(title);

        ws.Cell(1, 1).Value = BuildGeneratedLabel(DateTime.Now);
        ws.Cell(1, 1).Style.Font.Italic = true;

        var headers = new[] { "Class", "Name", "Student Info", "Local Contact", "Emergency Contact", "Permanent Contact" };

        for (int col = 0; col < headers.Length; col++)
        {
            ws.Cell(2, col + 1).Value = headers[col];
            ws.Cell(2, col + 1).Style.Font.Bold = true;
        }

        for (int i = 0; i < data.Count; i++)
        {
            var row = i + 3;
            var d = data[i];

            ws.Cell(row, 1).Value = ExcelHelper.SanitizeStringCell(d.ClassLevel);
            ws.Cell(row, 2).Value = ExcelHelper.SanitizeStringCell(d.FullName);
            ws.Cell(row, 3).Value = ExcelHelper.SanitizeStringCell(FormatStudentInfoBlock(d));
            ws.Cell(row, 3).Style.Alignment.WrapText = true;
            ws.Cell(row, 4).Value = ExcelHelper.SanitizeStringCell(FormatContactBlock(d.LocalContact));
            ws.Cell(row, 4).Style.Alignment.WrapText = true;
            ws.Cell(row, 5).Value = ExcelHelper.SanitizeStringCell(FormatContactBlock(d.EmergencyContact));
            ws.Cell(row, 5).Style.Alignment.WrapText = true;
            ws.Cell(row, 6).Value = ExcelHelper.SanitizeStringCell(FormatContactBlock(d.PermanentContact));
            ws.Cell(row, 6).Style.Alignment.WrapText = true;
        }

        if (data.Count > 0)
        {
            ExcelAccessibilityHelper.PromoteToAccessibleTable(
                ws.Range(2, 1, data.Count + 2, headers.Length),
                "EmergencyContactReport");
        }

        ws.Columns().AdjustToContents();

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    public byte[] GeneratePdf(List<StudentContactReportDto> data)
    {
        var generatedLabel = BuildGeneratedLabel(DateTime.Now);
        const string title = "Emergency Contact Report";

        var document = Document.Create(container =>
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

                page.Content().SemanticTable().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();   // Class
                        columns.RelativeColumn(2);   // Name
                        columns.RelativeColumn(2.5f); // Student Info
                        columns.RelativeColumn(2.5f); // Local Contact
                        columns.RelativeColumn(2.5f); // Emergency Contact
                        columns.RelativeColumn(2.5f); // Permanent Contact
                    });

                    var hdrStyle = TextStyle.Default.FontSize(8).SemiBold();
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Class").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Name").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Student Info").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Local Contact").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Emergency Contact").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Permanent Contact").Style(hdrStyle);
                    });

                    foreach (var d in data)
                    {
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.ClassLevel));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(OrDash(d.FullName));
                        PdfMultiLineCell(table, FormatStudentInfoLines(d));
                        PdfMultiLineCell(table, FormatContactLines(d.LocalContact));
                        PdfMultiLineCell(table, FormatContactLines(d.EmergencyContact));
                        PdfMultiLineCell(table, FormatContactLines(d.PermanentContact));
                    }
                });

                page.Footer().SemanticIgnore().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        })
        .WithAccessibility(title, subject: "Student emergency contact report");

        return document.GeneratePdf();
    }

    private static string CompletenessLabel(int complete, int total)
    {
        if (complete >= total) return "Yes";
        if (complete == 0) return "No";
        return "Partial";
    }

    /// <summary>
    /// Returns an em-dash placeholder when the value is null or empty so that
    /// QuestPDF emits the cell as a tagged TD (whitespace-only and empty Text
    /// elements are dropped from the structure tree, breaking PDF/UA clause 7.2).
    /// </summary>
    private static string OrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    /// <summary>Formats student info as a multi-line block for Excel cells.</summary>
    private static string FormatStudentInfoBlock(StudentContactReportDto d)
    {
        return string.Join("\n", FormatStudentInfoLines(d));
    }

    private static List<string> FormatStudentInfoLines(StudentContactReportDto d)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(d.Address)) lines.Add(d.Address);
        var cityZip = string.Join(" ", new[] { d.City, d.Zip }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrEmpty(cityZip)) lines.Add(cityZip);
        if (!string.IsNullOrWhiteSpace(d.HomePhone)) lines.Add($"H: {d.HomePhone}");
        if (!string.IsNullOrWhiteSpace(d.CellPhone)) lines.Add($"C: {d.CellPhone}");
        return lines;
    }

    /// <summary>Formats a contact as a multi-line block for Excel cells.</summary>
    private static string FormatContactBlock(ContactInfoDto contact)
    {
        return string.Join("\n", FormatContactLines(contact));
    }

    private static List<string> FormatContactLines(ContactInfoDto contact)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(contact.Name)) lines.Add(contact.Name);
        if (!string.IsNullOrWhiteSpace(contact.Relationship)) lines.Add(contact.Relationship);
        if (!string.IsNullOrWhiteSpace(contact.WorkPhone)) lines.Add($"W: {contact.WorkPhone}");
        if (!string.IsNullOrWhiteSpace(contact.HomePhone)) lines.Add($"H: {contact.HomePhone}");
        if (!string.IsNullOrWhiteSpace(contact.CellPhone)) lines.Add($"C: {contact.CellPhone}");
        if (!string.IsNullOrWhiteSpace(contact.Email)) lines.Add($"E: {contact.Email}");
        return lines;
    }

    private static void PdfMultiLineCell(TableDescriptor table, List<string> lines)
    {
        table.Cell().BorderBottom(0.5f).Padding(2).Column(col =>
        {
            if (lines.Count == 0)
            {
                // Empty Column children would cause QuestPDF to drop the cell from the
                // tag tree, breaking PDF/UA clause 7.2 (rows must have matching column counts).
                col.Item().Text("—");
            }
            else
            {
                foreach (var line in lines)
                {
                    col.Item().Text(line);
                }
            }
        });
    }

}
