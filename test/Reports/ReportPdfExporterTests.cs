using System.Text;
using QuestPDF.Infrastructure;
using Viper.Areas.Reports.Exporters;

namespace Viper.test.Reports;

/// <summary>
/// QuestPDF output can't be read back without a PDF parser, so these tests render every layout
/// path (groups, row numbers, notes, fills, totals, pivots, charts, header variants) and check
/// that a PDF comes out. The layout decisions themselves are unit-tested in
/// <see cref="ReportExportTextTests"/>.
/// </summary>
public sealed class ReportPdfExporterTests
{
    private readonly ReportPdfExporter _exporter = new();

    public ReportPdfExporterTests()
    {
        // Program.cs sets this at startup; unit tests don't run Program.cs.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static void AssertIsPdf(byte[] bytes)
    {
        Assert.True(bytes.Length > 0);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public void Describes_ItsFormat()
    {
        Assert.Equal("pdf", _exporter.Format);
        Assert.Equal("application/pdf", _exporter.ContentType);
        Assert.Equal(".pdf", _exporter.Extension);
    }

    [Fact]
    public void Full_WithFiltersAndNotice_RendersPdf()
    {
        AssertIsPdf(_exporter.Export(ReportExportTestData.Export(
            ReportExportTestData.Full(), confidential: true, filters: ReportExportTestData.Filters)));
    }

    [Fact]
    public void Plain_RendersPdf()
    {
        AssertIsPdf(_exporter.Export(ReportExportTestData.Export(ReportExportTestData.Plain())));
    }

    [Fact]
    public void EdgeCases_RenderPdf()
    {
        AssertIsPdf(_exporter.Export(ReportExportTestData.Export(ReportExportTestData.EdgeCases())));
    }

    [Fact]
    public void NullExport_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _exporter.Export(null!));
    }
}
