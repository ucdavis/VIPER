using ClosedXML.Excel;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Exporters;

namespace Viper.test.Reports;

public sealed class ReportExcelExporterTests
{
    private readonly ReportExcelExporter _exporter = new();

    private MemoryStream Export(ReportExport export)
    {
        return new MemoryStream(_exporter.Export(export));
    }

    private static IXLCell FindCell(IXLWorksheet sheet, string text)
    {
        return sheet.CellsUsed().First(cell => cell.GetString() == text);
    }

    private static int Argb(string html)
    {
        return XLColor.FromHtml(html).Color.ToArgb();
    }

    private static int FillArgb(IXLCell cell)
    {
        return cell.Style.Fill.BackgroundColor.Color.ToArgb();
    }

    [Fact]
    public void Describes_ItsFormat()
    {
        Assert.Equal("xlsx", _exporter.Format);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", _exporter.ContentType);
        Assert.Equal(".xlsx", _exporter.Extension);
    }

    [Fact]
    public void Header_ShowsTitleGeneratedFiltersAndNotice()
    {
        using MemoryStream stream = Export(ReportExportTestData.Export(
            ReportExportTestData.Full(), confidential: true, filters: ReportExportTestData.Filters));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet(1);

        Assert.Equal("Sample report", sheet.Name);
        Assert.Equal("Sample report", sheet.Cell(1, 1).GetString());
        Assert.True(sheet.Cell(1, 1).Style.Font.Bold);
        Assert.Equal(ReportExportText.Generated(TestReport.GeneratedAt), sheet.Cell(2, 1).GetString());
        Assert.Equal("Filters: Faculty type: Senate; Start date: 07/01/2026", sheet.Cell(3, 1).GetString());
        Assert.Equal(ReportExportText.ConfidentialNotice, sheet.Cell(4, 1).GetString());
        Assert.Equal("Sample report", workbook.Properties.Title);
    }

    [Fact]
    public void Header_WithoutFiltersOrNotice_StartsDataSooner()
    {
        using MemoryStream stream = Export(ReportExportTestData.Export(ReportExportTestData.Plain()));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet(1);

        Assert.Equal("Name", sheet.Cell(4, 1).GetString());
        Assert.Equal("Ada", sheet.Cell(5, 1).GetString());
        Assert.Equal(120_000d, sheet.Cell(5, 3).GetDouble());
        Assert.Equal("$#,##0.00", sheet.Cell(5, 3).Style.NumberFormat.Format);
        Assert.True(sheet.Cell(7, 3).IsEmpty());
        Assert.Single(sheet.Tables);
        Assert.Single(workbook.Worksheets);
    }

    [Fact]
    public void Groups_WriteLabelsTypedRowsSubtotalsAndTotals()
    {
        using MemoryStream stream = Export(ReportExportTestData.Export(ReportExportTestData.Full()));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet(1);

        IXLCell people = FindCell(sheet, "People");
        Assert.Equal(5d, people.CellRight().GetDouble());

        IXLCell vme = FindCell(sheet, "VME");
        IXLCell header = vme.CellBelow();
        Assert.Equal("#", header.GetString());
        Assert.Equal("Notes", header.WorksheetRow().Cell(7).GetString());

        IXLRow ada = header.WorksheetRow().RowBelow();
        Assert.Equal(1d, ada.Cell(1).GetDouble());
        Assert.Equal("Ada", ada.Cell(2).GetString());
        Assert.Equal(61d, ada.Cell(3).GetDouble());
        Assert.Equal(new DateTime(1995, 7, 1), ada.Cell(5).GetDateTime());
        Assert.Equal("mm/dd/yyyy", ada.Cell(5).Style.NumberFormat.Format);
        Assert.Equal("Asian, White", ada.Cell(6).GetString());

        IXLCell subtotal = sheet.CellsUsed().Last(cell => cell.GetString() == "Salaries");
        Assert.Equal(270_000d, subtotal.WorksheetRow().Cell(4).GetDouble());
        IXLCell total = FindCell(sheet, "All salaries");
        Assert.Equal(430_000d, total.WorksheetRow().Cell(4).GetDouble());
        Assert.Equal(3, sheet.Tables.Count());
    }

    [Fact]
    public void Highlights_FillRowsAndCellsAndListLabelsInNotes()
    {
        using MemoryStream stream = Export(ReportExportTestData.Export(ReportExportTestData.Full()));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet(1);

        IXLRow ada = FindCell(sheet, "Ada").WorksheetRow();
        Assert.Equal(Argb("#FFF4CE"), FillArgb(ada.Cell(3)));
        Assert.Equal("60+", ada.Cell(7).GetString());

        IXLRow di = FindCell(sheet, "Di").WorksheetRow();
        Assert.Equal(Argb("#EDEBE9"), FillArgb(di.Cell(2)));
        Assert.Equal("Retirement age; 60+", di.Cell(7).GetString());

        IXLRow ed = FindCell(sheet, "Ed").WorksheetRow();
        Assert.Equal("Under 30", ed.Cell(7).GetString());
    }

    [Fact]
    public void PivotsAndCharts_GetTheirOwnSheets()
    {
        using MemoryStream stream = Export(ReportExportTestData.Export(ReportExportTestData.Full()));
        using var workbook = new XLWorkbook(stream);

        Assert.Equal(["Sample report", "Sample report (2)", "Charts"], workbook.Worksheets.Select(sheet => sheet.Name));

        IXLWorksheet pivot = workbook.Worksheet(2);
        Assert.Equal("Total", pivot.Cell(2, 5).GetString());
        Assert.Equal("60+", pivot.Cell(3, 1).GetString());
        Assert.Equal(2d, pivot.Cell(3, 4).GetDouble());
        Assert.Equal(5d, pivot.Cell(5, 5).GetDouble());

        IXLWorksheet charts = workbook.Worksheet(3);
        Assert.Equal("Ages", charts.Cell(1, 1).GetString());
        Assert.Equal("Department ages: VME", FindCell(charts, "Department ages: VME").GetString());
    }

    [Fact]
    public void EdgeCases_AreHandledWithoutErrors()
    {
        using MemoryStream stream = Export(ReportExportTestData.Export(ReportExportTestData.EdgeCases()));
        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet(1);

        // ClosedXML stores a leading apostrophe as Excel's quote prefix rather than as text, so
        // find the row through its header and check the value can't run as a formula.
        IXLRow row = FindCell(sheet, "Name").CellBelow().WorksheetRow();
        Assert.EndsWith("=cmd|' /C calc'!A0", row.Cell(1).GetString(), StringComparison.Ordinal);
        Assert.False(row.Cell(1).HasFormula);
        Assert.Equal(new DateTime(2026, 1, 2), row.Cell(2).GetDateTime());
        Assert.Equal(0.125d, row.Cell(3).GetDouble());
        Assert.Equal("0.0%", row.Cell(3).Style.NumberFormat.Format);
        Assert.Equal("pending", row.Cell(4).GetString());
        Assert.Equal(Argb("#FDE7E9"), FillArgb(row.Cell(1)));

        Assert.Equal(3d, FindCell(sheet, "Unplaced").CellRight().GetDouble());
        Assert.True(FindCell(sheet, "Unknown").CellRight().IsEmpty());
        Assert.Single(sheet.Tables);
    }

    [Fact]
    public void NullExport_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _exporter.Export(null!));
    }

    [Fact]
    public void UniqueSheetName_AddsNumberedSuffixWithinTheLengthLimit()
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string longName = new('x', 40);

        Assert.Equal("Report", ReportExcelExporter.UniqueSheetName("Report", used));
        Assert.Equal("Report (2)", ReportExcelExporter.UniqueSheetName("Report", used));
        Assert.Equal("Report (3)", ReportExcelExporter.UniqueSheetName("Report", used));
        Assert.Equal(new string('x', 31), ReportExcelExporter.UniqueSheetName(longName, used));
        Assert.Equal(new string('x', 27) + " (2)", ReportExcelExporter.UniqueSheetName(longName, used));
    }

    [Theory]
    [InlineData(ReportColumnFormat.Number, null, "#,##0")]
    [InlineData(ReportColumnFormat.Number, 2, "#,##0.00")]
    [InlineData(ReportColumnFormat.Currency, 2, "$#,##0.00")]
    [InlineData(ReportColumnFormat.Percent, 0, "0%")]
    [InlineData(ReportColumnFormat.Percent, 1, "0.0%")]
    public void NumberFormat_MatchesColumnFormat(ReportColumnFormat format, int? decimals, string expected)
    {
        var column = new ReportColumnMetadata("value", "Value", format, ReportAlignment.Right, decimals);

        Assert.Equal(expected, ReportExcelExporter.NumberFormat(column));
    }
}
