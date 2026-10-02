using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Exporters;

namespace Viper.test.Reports;

public sealed class ReportExportTextTests
{
    private static readonly HashSet<string> Columns = new(["name", "age"], StringComparer.Ordinal);

    private static readonly ReportColumnMetadata[] SalaryColumn =
    [
        new("salary", "Salary", ReportColumnFormat.Currency, ReportAlignment.Right, 2),
    ];

    private static ReportFlag Highlight(ReportTone tone, string? columnKey = null)
    {
        return new ReportFlag(ReportFlagKind.Highlight, tone, tone.ToString(), columnKey);
    }

    [Fact]
    public void Generated_UsesUsDateAndTime()
    {
        Assert.Equal("Generated 09/29/2026 8:00 AM", ReportExportText.Generated(TestReport.GeneratedAt));
    }

    [Fact]
    public void Filters_JoinsLabelsOrReturnsNull()
    {
        Assert.Null(ReportExportText.Filters([]));
        Assert.Equal(
            "Filters: Faculty type: Senate; Start date: 07/01/2026",
            ReportExportText.Filters(ReportExportTestData.Filters));
        Assert.Throws<ArgumentNullException>(() => ReportExportText.Filters(null!));
    }

    [Fact]
    public void HasNotes_NullResult_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ReportExportText.HasNotes(null!));
    }

    [Theory]
    [InlineData(ReportTone.Neutral, null)]
    [InlineData(ReportTone.Warning, "#FFF4CE")]
    [InlineData(ReportTone.Muted, "#EDEBE9")]
    public void ToneFill_MapsTonesToLightFills(ReportTone tone, string? expected)
    {
        Assert.Equal(expected, ReportExportText.ToneFill(tone));
    }

    [Fact]
    public void CellFill_RowHighlightCoversEveryCell()
    {
        ReportFlag[] flags = [Highlight(ReportTone.Muted)];

        Assert.Equal("#EDEBE9", ReportExportText.CellFill(flags, "name", Columns));
        Assert.Equal("#EDEBE9", ReportExportText.CellFill(flags, null, Columns));
    }

    [Fact]
    public void CellFill_ColumnHighlightCoversOnlyItsColumn()
    {
        ReportFlag[] flags = [Highlight(ReportTone.Muted), Highlight(ReportTone.Warning, "age")];

        Assert.Equal("#FFF4CE", ReportExportText.CellFill(flags, "age", Columns));
        Assert.Equal("#EDEBE9", ReportExportText.CellFill(flags, "name", Columns));
        Assert.Equal("#EDEBE9", ReportExportText.CellFill(flags, null, Columns));
    }

    [Fact]
    public void CellFill_HighlightOnUnknownColumnCoversTheRow()
    {
        Assert.Equal("#FDE7E9", ReportExportText.CellFill([Highlight(ReportTone.Negative, "missing")], "name", Columns));
    }

    [Fact]
    public void CellFill_IgnoresBadgesAndNeutralHighlights()
    {
        ReportFlag[] flags =
        [
            Highlight(ReportTone.Info),
            new ReportFlag(ReportFlagKind.Badge, ReportTone.Warning, "Badge", "name"),
            Highlight(ReportTone.Neutral),
        ];

        Assert.Equal("#DEECF9", ReportExportText.CellFill(flags, "name", Columns));
        Assert.Null(ReportExportText.CellFill([], "name", Columns));
    }

    [Fact]
    public void CellFill_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ReportExportText.CellFill(null!, "name", Columns));
        Assert.Throws<ArgumentNullException>(() => ReportExportText.CellFill([], "name", null!));
    }

    [Fact]
    public void FormatTotal_UsesTheColumnFormatWhenThereIsOne()
    {
        Assert.Equal("$1,234.50", ReportExportText.FormatTotal(new ReportTotal("Salaries", "salary", 1234.5m), SalaryColumn));
        Assert.Equal("1,234.5", ReportExportText.FormatTotal(new ReportTotal("People", null, 1234.5m), SalaryColumn));
        Assert.Equal("7", ReportExportText.FormatTotal(new ReportTotal("Other", "missing", 7m), SalaryColumn));
        Assert.Equal(string.Empty, ReportExportText.FormatTotal(new ReportTotal("None", "salary", null), SalaryColumn));
    }

    [Fact]
    public void FormatTotal_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ReportExportText.FormatTotal(null!, SalaryColumn));
        Assert.Throws<ArgumentNullException>(() => ReportExportText.FormatTotal(new ReportTotal("T", null, 1m), null!));
    }

    [Fact]
    public void FormatNumber_BlankForNull()
    {
        Assert.Equal(string.Empty, ReportExportText.FormatNumber(null));
        Assert.Equal("12,345.68", ReportExportText.FormatNumber(12345.678m));
    }
}
