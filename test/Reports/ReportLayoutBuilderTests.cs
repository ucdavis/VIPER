using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportLayoutBuilderTests
{
    #region Argument validation

    [Fact]
    public void GroupBy_Twice_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        builder.GroupBy(row => row.Department);

        Assert.Throws<InvalidOperationException>(() => builder.GroupBy(row => row.Name));
    }

    [Fact]
    public void GroupBy_NullKey_Throws()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.GroupBy(null!));
    }

    [Fact]
    public void Totals_InvalidArguments_Throw()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentException>(() => builder.Total(" ", builder.Count()));
        Assert.Throws<ArgumentNullException>(() => builder.Summary("People", null!));
        Assert.Throws<ArgumentException>(() => builder.GroupBy(row => row.Department).Subtotal("", builder.Count()));
    }

    [Fact]
    public void Flags_InvalidArguments_Throw()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();

        Assert.Throws<ArgumentNullException>(() => builder.Highlight(null!, ReportTone.Warning, "Old"));
        Assert.Throws<ArgumentException>(() => builder.Highlight(_ => true, ReportTone.Warning, " "));
        Assert.Throws<ArgumentException>(() => builder.Badge(_ => true, ReportTone.Info, "Emeritus", " "));
    }

    [Fact]
    public void Pivots_InvalidArguments_Throw()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        ReportMeasure<SampleRow> count = builder.Count();

        Assert.Throws<ArgumentNullException>(() => builder.Pivot("Pivot", null!, row => row.Name, count));
        Assert.Throws<ArgumentNullException>(() => builder.PivotEach("Pivot", null!, row => row.Name, count));
        Assert.Throws<ArgumentException>(() => builder.Pivot(" ", row => row.Name, row => row.Name, count));
        Assert.Throws<ArgumentNullException>(() => builder.Pivot("Pivot", row => row.Name, null!, count));
        Assert.Throws<ArgumentNullException>(() => builder.Pivot("Pivot", row => row.Name, row => row.Name, null!));
    }

    [Fact]
    public void Charts_InvalidArguments_Throw()
    {
        var builder = new ReportBuilder<SampleRow, SampleParams>();
        ReportMeasure<SampleRow> count = builder.Count();

        Assert.Throws<ArgumentException>(() => builder.Chart(" ", ReportChartKind.Bar, row => row.Name, count));
        Assert.Throws<ArgumentNullException>(() => builder.Chart("Chart", ReportChartKind.Bar, null!, count));
        Assert.Throws<ArgumentNullException>(() => builder.Chart("Chart", ReportChartKind.Bar, row => row.Name, null!));

        ReportChartBuilder<SampleRow> chart = builder.Chart("Chart", ReportChartKind.Pie, row => row.Name, count);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.PerGroup(-1));
        Assert.Throws<ArgumentNullException>(() => chart.CategoryOrder(null!));
    }

    #endregion

    #region Layout validation

    [Fact]
    public void UnknownTotalColumn_Throws()
    {
        var report = new TestReport(report =>
        {
            report.Column(row => row.Name, "Name");
            report.Total("Total", report.Count(), "missing");
        });

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    [Fact]
    public void UnknownSummaryColumn_Throws()
    {
        var report = new TestReport(report =>
        {
            report.Column(row => row.Name, "Name");
            report.Summary("People", report.Count(), "missing");
        });

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    [Fact]
    public void UnknownSubtotalColumn_Throws()
    {
        var report = new TestReport(report =>
        {
            report.Column(row => row.Name, "Name");
            report.GroupBy(row => row.Department).Subtotal("People", report.Count(), "missing");
        });

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    [Fact]
    public void UnknownFlagColumn_Throws()
    {
        var report = new TestReport(report =>
        {
            report.Column(row => row.Name, "Name");
            report.Badge(_ => true, ReportTone.Info, "Badge", "missing");
        });

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    [Fact]
    public void PerGroupChartWithoutGrouping_Throws()
    {
        var report = new TestReport(report =>
        {
            report.Column(row => row.Name, "Name");
            report.Chart("Ages", ReportChartKind.Bar, row => row.Name, report.Count()).PerGroup();
        });

        Assert.Throws<InvalidOperationException>(() => report.GetMetadata(TestReport.ContextWith()));
    }

    [Fact]
    public void KnownColumnsAndGroupedCharts_AreValid()
    {
        var report = new TestReport(report =>
        {
            report.Column(row => row.Name, "Name");
            report.Column(row => row.Salary, "Salary");
            report.Total("Salaries", report.Sum(row => row.Salary), "salary");
            report.Highlight(row => row.Age > 60, ReportTone.Warning, "Over 60");
            report.GroupBy(row => row.Department).Subtotal("People", report.Count());
            report.Chart("Ages", ReportChartKind.Bar, row => row.Name, report.Count()).PerGroup(2);
        });

        ReportDefinitionMetadata metadata = report.GetMetadata(TestReport.ContextWith());

        Assert.Equal(2, metadata.Columns.Count);
    }

    #endregion
}
