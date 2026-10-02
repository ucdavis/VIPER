using System.Text;
using Viper.Areas.Reports.Exporters;

namespace Viper.test.Reports;

public sealed class ReportCsvExporterTests
{
    private readonly ReportCsvExporter _exporter = new();

    private string[] Lines(ReportExport export)
    {
        byte[] bytes = _exporter.Export(export);
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        string text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        Assert.EndsWith("\r\n", text, StringComparison.Ordinal);
        return text[..^2].Split("\r\n");
    }

    [Fact]
    public void Describes_ItsFormat()
    {
        Assert.Equal("csv", _exporter.Format);
        Assert.Equal("text/csv", _exporter.ContentType);
        Assert.Equal(".csv", _exporter.Extension);
    }

    [Fact]
    public void Plain_WritesHeaderAndOneLinePerRow()
    {
        string[] lines = Lines(ReportExportTestData.Export(ReportExportTestData.Plain()));

        Assert.Equal("Name,Department,Salary", lines[0]);
        Assert.Equal("Ada,VME,\"$120,000.00\"", lines[1]);
        Assert.Equal("Cy,VME ,", lines[3]);
        Assert.Equal(6, lines.Length);
    }

    [Fact]
    public void Grouped_AddsGroupAndNotesColumns()
    {
        string[] lines = Lines(ReportExportTestData.Export(ReportExportTestData.Full(), confidential: true));

        Assert.Equal("Group,Name,Age,Salary,Hire date,Ethnicities,Notes", lines[0]);
        Assert.Equal("(Not specified),Ed,29,\"$70,000.00\",08/15/2022,White,Under 30", lines[1]);
        Assert.Equal("VME,Ada,61,\"$120,000.00\",07/01/1995,\"Asian, White\",60+", lines[3]);
        Assert.Equal("VME,Cy,38,,09/01/2018,Hispanic,No salary", lines[4]);
        Assert.Equal("VME,Di,66,\"$150,000.00\",03/01/1990,,Retirement age; 60+", lines[5]);
    }

    [Fact]
    public void EdgeCases_SanitizeFormulasAndKeepTextInNumericColumns()
    {
        string[] lines = Lines(ReportExportTestData.Export(ReportExportTestData.EdgeCases()));

        Assert.Equal("Name,When,Share,Amount,Notes", lines[0]);
        Assert.StartsWith("'=cmd|' /C calc'!A0,01/02/2026,", lines[1], StringComparison.Ordinal);
        Assert.EndsWith(",pending,Check", lines[1], StringComparison.Ordinal);
        Assert.Equal(2, lines.Length);
    }

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("line\nbreak", "\"line\nbreak\"")]
    public void Quote_FollowsRfc4180(string value, string expected)
    {
        Assert.Equal(expected, ReportCsvExporter.Quote(value));
    }

    [Fact]
    public void NullExport_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _exporter.Export(null!));
    }
}
