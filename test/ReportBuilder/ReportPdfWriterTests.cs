using System.Text;
using QuestPDF.Infrastructure;
using Viper.Areas.ReportBuilder.Exporters;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.test.ReportBuilder;

/// <summary>
/// QuestPDF output can't be read back without a PDF parser, so these tests render each layout
/// path (heading variants, both orientations, empty cells, numeric alignment) and check that a
/// PDF comes out. The text in the cells is tested in <see cref="ReportCellFormatterTests"/>.
/// </summary>
public sealed class ReportPdfWriterTests
{
    public ReportPdfWriterTests()
    {
        // Program.cs sets this at startup; unit tests don't run Program.cs.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static ReportExportDocument Document(bool heading, bool landscape)
    {
        return new ReportExportDocument
        {
            Title = "Faculty roster",
            GeneratedAt = new DateTimeOffset(2026, 11, 16, 9, 5, 0, TimeSpan.Zero),
            Filters = heading ? [new ReportAppliedFilter { Label = "Department", Value = "Pathology" }] : [],
            SensitiveNotice = heading ? "Confidential" : null,
            Truncated = heading,
            Landscape = landscape,
            Columns =
            [
                new ReportResultColumn { Name = "Name", Label = "Name", Format = ReportColumnFormat.Text, Sortable = true },
                new ReportResultColumn { Name = "Salary", Label = "Salary", Format = ReportColumnFormat.Currency, Sortable = true },
            ],
            Rows =
            [
                FakeReportQueryExecutor.Row(("Name", "Ada"), ("Salary", 1234.5m)),
                FakeReportQueryExecutor.Row(("Name", null)),
            ],
        };
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Write_RendersAPdf(bool heading, bool landscape)
    {
        byte[] pdf = ReportPdfWriter.Write(Document(heading, landscape));

        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }

    [Fact]
    public void NullDocument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ReportPdfWriter.Write(null!));
    }
}
