using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Students.Models;
using Viper.Classes.Utilities;

namespace Viper.Areas.Students.Services;

public interface IEmergencyContactExportService
{
    MemoryStream GenerateExcel(List<StudentContactReportDto> data);
    byte[] GeneratePdf(List<StudentContactReportDto> data);
}

public class EmergencyContactExportService : IEmergencyContactExportService
{
    public MemoryStream GenerateExcel(List<StudentContactReportDto> data)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Emergency Contacts");

        // Header row
        var headers = new[]
        {
            "Name", "Class", "Address", "City", "Zip", "Home Phone", "Cell Phone",
            "Local Name", "Local Relationship", "Local Work Phone", "Local Home Phone",
            "Local Cell Phone", "Local Email",
            "Emergency Name", "Emergency Relationship", "Emergency Work Phone",
            "Emergency Home Phone", "Emergency Cell Phone", "Emergency Email",
            "Permanent Name", "Permanent Relationship", "Permanent Work Phone",
            "Permanent Home Phone", "Permanent Cell Phone", "Permanent Email",
            "Contact Permanent?"
        };

        for (int col = 0; col < headers.Length; col++)
        {
            ws.Cell(1, col + 1).Value = headers[col];
            ws.Cell(1, col + 1).Style.Font.Bold = true;
        }

        // Data rows
        for (int i = 0; i < data.Count; i++)
        {
            var row = i + 2;
            var d = data[i];

            ws.Cell(row, 1).Value = ExcelHelper.SanitizeStringCell(d.FullName);
            ws.Cell(row, 2).Value = ExcelHelper.SanitizeStringCell(d.ClassLevel);
            ws.Cell(row, 3).Value = ExcelHelper.SanitizeStringCell(d.Address);
            ws.Cell(row, 4).Value = ExcelHelper.SanitizeStringCell(d.City);
            ws.Cell(row, 5).Value = ExcelHelper.SanitizeStringCell(d.Zip);
            ws.Cell(row, 6).Value = ExcelHelper.SanitizeStringCell(d.HomePhone);
            ws.Cell(row, 7).Value = ExcelHelper.SanitizeStringCell(d.CellPhone);

            WriteContactColumns(ws, row, 8, d.LocalContact);
            WriteContactColumns(ws, row, 14, d.EmergencyContact);
            WriteContactColumns(ws, row, 20, d.PermanentContact);

            ws.Cell(row, 26).Value = d.ContactPermanent ? "Yes" : "No";
        }

        ws.Columns().AdjustToContents();

        var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    public byte[] GeneratePdf(List<StudentContactReportDto> data)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(0.5f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Text("Emergency Contact Report")
                    .SemiBold().FontSize(14).AlignCenter();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Name
                        columns.RelativeColumn(1); // Class
                        columns.RelativeColumn(2); // Address
                        columns.RelativeColumn(2); // Home Phone
                        columns.RelativeColumn(2); // Cell Phone
                        columns.RelativeColumn(2); // Emergency Name
                        columns.RelativeColumn(1.5f); // Emergency Rel.
                        columns.RelativeColumn(2); // Emergency Phone
                    });

                    // Header
                    var hdrStyle = TextStyle.Default.FontSize(8).SemiBold();
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Name").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Class").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Address").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Home Phone").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Cell Phone").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Emerg. Name").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Emerg. Rel.").Style(hdrStyle);
                        header.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).Padding(2).Text("Emerg. Phone").Style(hdrStyle);
                    });

                    // Rows
                    foreach (var d in data)
                    {
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(d.FullName);
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(d.ClassLevel);
                        table.Cell().BorderBottom(0.5f).Padding(2)
                            .Text($"{d.Address ?? ""}, {d.City ?? ""} {d.Zip ?? ""}".Trim(' ', ','));
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(d.HomePhone ?? "");
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(d.CellPhone ?? "");
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(d.EmergencyContact.Name ?? "");
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(d.EmergencyContact.Relationship ?? "");
                        // Show best available emergency phone
                        var emergPhone = d.EmergencyContact.CellPhone
                            ?? d.EmergencyContact.HomePhone
                            ?? d.EmergencyContact.WorkPhone
                            ?? "";
                        table.Cell().BorderBottom(0.5f).Padding(2).Text(emergPhone);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void WriteContactColumns(IXLWorksheet ws, int row, int startCol, ContactInfoDto contact)
    {
        ws.Cell(row, startCol).Value = ExcelHelper.SanitizeStringCell(contact.Name);
        ws.Cell(row, startCol + 1).Value = ExcelHelper.SanitizeStringCell(contact.Relationship);
        ws.Cell(row, startCol + 2).Value = ExcelHelper.SanitizeStringCell(contact.WorkPhone);
        ws.Cell(row, startCol + 3).Value = ExcelHelper.SanitizeStringCell(contact.HomePhone);
        ws.Cell(row, startCol + 4).Value = ExcelHelper.SanitizeStringCell(contact.CellPhone);
        ws.Cell(row, startCol + 5).Value = ExcelHelper.SanitizeStringCell(contact.Email);
    }

}
