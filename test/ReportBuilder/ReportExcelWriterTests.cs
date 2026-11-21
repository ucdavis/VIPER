using ClosedXML.Excel;
using Viper.Areas.ReportBuilder.Exporters;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.test.ReportBuilder;

/// <summary>The Excel download: heading, an accessible table, typed numbers and dates, and safe text.</summary>
public sealed class ReportExcelWriterTests
{
    private static readonly DateTimeOffset GeneratedAt = new(2026, 11, 16, 9, 5, 0, TimeSpan.Zero);
    private static readonly DateTime Hired = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Local);
    private static readonly DateTime Updated = new(2026, 3, 1, 14, 5, 0, DateTimeKind.Local);

    private static ReportResultColumn Column(string name, ReportColumnFormat format, string? label = null)
    {
        return new ReportResultColumn { Name = name, Label = label ?? name, Format = format, Sortable = true };
    }

    private static ReportExportDocument Document(bool heading = true)
    {
        return new ReportExportDocument
        {
            Title = "Faculty roster",
            GeneratedAt = GeneratedAt,
            Filters = heading ? [new ReportAppliedFilter { Label = "Department", Value = "Pathology" }] : [],
            SensitiveNotice = heading ? "Confidential" : null,
            Truncated = heading,
            Columns =
            [
                Column("Name", ReportColumnFormat.Text),
                Column("Count", ReportColumnFormat.Number),
                Column("Salary", ReportColumnFormat.Currency),
                Column("Share", ReportColumnFormat.Percent),
                Column("Hired", ReportColumnFormat.Date),
                Column("Updated", ReportColumnFormat.DateTime),
                Column("Active", ReportColumnFormat.YesNo),
                Column("Notes", ReportColumnFormat.Text, "Name"),
            ],
            Rows =
            [
                FakeReportQueryExecutor.Row(("Name", "=SUM(A1)"), ("Count", 3), ("Salary", 1234.5m), ("Share", 0.25m),
                    ("Hired", Hired), ("Updated", Updated), ("Active", true), ("Notes", Updated)),
                FakeReportQueryExecutor.Row(("Name", "Bo"), ("Count", 4), ("Salary", null), ("Share", 0.5m), ("Active", false)),
            ],
        };
    }

    private static XLWorkbook Open(ReportExportDocument document)
    {
        return new XLWorkbook(new MemoryStream(ReportExcelWriter.Write(document)));
    }

    [Fact]
    public void Heading_ShowsTitleGeneratedFiltersNoticeAndTruncation()
    {
        using XLWorkbook workbook = Open(Document());
        IXLWorksheet sheet = workbook.Worksheet(1);

        Assert.Equal("Faculty roster", sheet.Name);
        Assert.Equal("Faculty roster", workbook.Properties.Title);
        Assert.Equal("Faculty roster", sheet.Cell(1, 1).GetString());
        Assert.True(sheet.Cell(1, 1).Style.Font.Bold);
        Assert.Equal("Generated 11/16/2026 9:05 AM", sheet.Cell(2, 1).GetString());
        Assert.Equal("Filters: Department: Pathology", sheet.Cell(3, 1).GetString());
        Assert.Equal("Confidential", sheet.Cell(4, 1).GetString());
        Assert.True(sheet.Cell(4, 1).Style.Font.Italic);
        Assert.Equal("Only the first 50,000 rows are included.", sheet.Cell(5, 1).GetString());
        Assert.Equal("Name", sheet.Cell(7, 1).GetString());
    }

    [Fact]
    public void Heading_WithoutExtras_StartsTheTableSooner()
    {
        using XLWorkbook workbook = Open(Document(heading: false));
        IXLWorksheet sheet = workbook.Worksheet(1);

        Assert.Equal("Name", sheet.Cell(4, 1).GetString());
        Assert.Equal("Bo", sheet.Cell(6, 1).GetString());
    }

    [Fact]
    public void Rows_AreAnAccessibleTableWithUniqueHeaders()
    {
        using XLWorkbook workbook = Open(Document(heading: false));
        IXLWorksheet sheet = workbook.Worksheet(1);

        IXLTable table = Assert.Single(sheet.Tables);
        Assert.Equal("ReportTable", table.Name);
        Assert.Equal("Name (2)", sheet.Cell(4, 8).GetString());
        Assert.True(sheet.Cell(4, 1).Style.Font.Bold);
    }

    [Fact]
    public void Values_StayTypedWithFormatsByColumn()
    {
        using XLWorkbook workbook = Open(Document(heading: false));
        IXLWorksheet sheet = workbook.Worksheet(1);

        // A value that looks like a formula stays text. ClosedXML may keep the sanitizing
        // apostrophe in the text or turn it into the cell's quote prefix, so check the outcome.
        IXLCell formulaLike = sheet.Cell(5, 1);
        Assert.False(formulaLike.HasFormula);
        Assert.Equal(XLDataType.Text, formulaLike.DataType);
        Assert.EndsWith("=SUM(A1)", formulaLike.GetString(), StringComparison.Ordinal);
        Assert.Equal(3d, sheet.Cell(5, 2).GetDouble());
        Assert.Equal("#,##0", sheet.Cell(5, 2).Style.NumberFormat.Format);
        Assert.Equal(1234.5d, sheet.Cell(5, 3).GetDouble());
        Assert.Equal("$#,##0.00", sheet.Cell(5, 3).Style.NumberFormat.Format);
        Assert.True(sheet.Cell(6, 3).IsEmpty());
        Assert.Equal(0.25d, sheet.Cell(5, 4).GetDouble());
        Assert.Equal("0%", sheet.Cell(5, 4).Style.NumberFormat.Format);
        Assert.Equal(Hired, sheet.Cell(5, 5).GetDateTime());
        Assert.Equal("mm/dd/yyyy", sheet.Cell(5, 5).Style.NumberFormat.Format);
        Assert.Equal(Updated, sheet.Cell(5, 6).GetDateTime());
        Assert.Equal("mm/dd/yyyy h:mm AM/PM", sheet.Cell(5, 6).Style.NumberFormat.Format);
        Assert.Equal("Yes", sheet.Cell(5, 7).GetString());
        Assert.Equal("No", sheet.Cell(6, 7).GetString());
        Assert.Equal("mm/dd/yyyy h:mm AM/PM", sheet.Cell(5, 8).Style.NumberFormat.Format);
    }

    [Fact]
    public void FractionalNumbers_GetTwoDecimals()
    {
        ReportExportDocument document = Document(heading: false) with
        {
            Rows = [FakeReportQueryExecutor.Row(("Count", 2.5), ("Share", 0.255m))],
        };

        using XLWorkbook workbook = Open(document);
        IXLWorksheet sheet = workbook.Worksheet(1);

        Assert.Equal("#,##0.00", sheet.Cell(5, 2).Style.NumberFormat.Format);
        Assert.Equal("0.00%", sheet.Cell(5, 4).Style.NumberFormat.Format);
    }

    [Fact]
    public void NoRows_HasNoTable()
    {
        using XLWorkbook workbook = Open(Document(heading: false) with { Rows = [] });

        Assert.Empty(workbook.Worksheet(1).Tables);
    }

    [Theory]
    [InlineData(ReportColumnFormat.Number, true, "#,##0")]
    [InlineData(ReportColumnFormat.Number, false, "#,##0.00")]
    [InlineData(ReportColumnFormat.Currency, true, "$#,##0.00")]
    [InlineData(ReportColumnFormat.Percent, false, "0.00%")]
    [InlineData(ReportColumnFormat.Text, true, null)]
    [InlineData(ReportColumnFormat.Date, true, null)]
    public void NumberFormat_ByColumnFormat(ReportColumnFormat format, bool whole, string? expected)
    {
        Assert.Equal(expected, ReportExcelWriter.NumberFormat(format, whole));
    }

    [Fact]
    public void NullDocument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ReportExcelWriter.Write(null!));
    }
}
