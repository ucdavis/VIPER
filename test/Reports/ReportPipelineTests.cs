using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportPipelineTests
{
    private static void BasicColumns(ReportBuilder<SampleRow, SampleParams> report)
    {
        report.Column(row => row.Name, "Name");
        report.Column(row => row.Age, "Age").AsNumber();
        report.Column(row => row.Salary, "Salary").AsCurrency()
            .VisibleWhen(context => context.HasPermission(TestReport.SalaryPermission));
    }

    #region Ungrouped reports

    [Fact]
    public void Ungrouped_ReturnsOneUnlabelledGroupWithNumberedRows()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.RowNumbers();
        });

        ReportResult result = TestReport.Run(report);

        Assert.Equal("test.sample", result.Key);
        Assert.Equal("Sample report", result.Title);
        Assert.Equal(TestReport.GeneratedAt, result.GeneratedAt);
        Assert.Equal(5, result.RowCount);
        Assert.True(result.RowNumbers);
        ReportGroupResult group = Assert.Single(result.Groups);
        Assert.Null(group.Label);
        Assert.Equal([1, 2, 3, 4, 5], group.Rows.Select(row => row.Number));
        Assert.Equal("Ada", group.Rows[0].Values["name"]);
        Assert.Equal(120_000m, group.Rows[0].Values["salary"]);
        Assert.Empty(group.Subtotals);
        Assert.Empty(group.Charts);
    }

    [Fact]
    public void RowNumbers_DefaultToOff()
    {
        ReportResult result = TestReport.Run(new TestReport(BasicColumns));

        Assert.False(result.RowNumbers);
    }

    [Fact]
    public void HiddenColumn_RemovesItsValuesTotalsSummariesAndFlags()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.Summary("People", report.Count());
            report.Summary("Top salary", report.Maximum(row => row.Salary), "salary");
            report.Total("Ages", report.Sum(row => row.Age), "age");
            report.Total("Salaries", report.Sum(row => row.Salary), "salary");
            report.Highlight(row => row.Salary > 100_000m, ReportTone.Info, "High salary", "salary");
        });

        ReportResult result = TestReport.Run(report, TestReport.ContextWith(TestReport.RunPermission));

        Assert.Equal(["name", "age"], result.Columns.Select(column => column.Key));
        Assert.DoesNotContain("salary", result.Groups[0].Rows[0].Values.Keys);
        Assert.Equal([new ReportTotal("People", null, 5m)], result.Summary);
        Assert.Equal([new ReportTotal("Ages", "age", 239m)], result.Totals);
        Assert.All(result.Groups[0].Rows, row => Assert.Empty(row.Flags));
    }

    [Fact]
    public void Flags_MarkMatchingRowsAndCells()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.Highlight(row => row.Age >= 65, ReportTone.Muted, "Retirement age");
            report.Badge(row => row.Age >= 60, ReportTone.Warning, "60+", "age");
        });

        IReadOnlyList<ReportRowResult> rows = TestReport.Run(report).Groups[0].Rows;

        Assert.Equal([new ReportFlag(ReportFlagKind.Badge, ReportTone.Warning, "60+", "age")], rows[0].Flags);
        Assert.Empty(rows[1].Flags);
        Assert.Equal(
            [
                new ReportFlag(ReportFlagKind.Highlight, ReportTone.Muted, "Retirement age", null),
                new ReportFlag(ReportFlagKind.Badge, ReportTone.Warning, "60+", "age"),
            ],
            rows[3].Flags);
    }

    #endregion

    #region Grouping

    [Fact]
    public void Grouping_NormalizesKeysOrdersGroupsAndRestartsNumbering()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.RowNumbers();
            report.GroupBy(row => row.Department)
                .Subtotal("People", report.Count())
                .Subtotal("Salaries", report.Sum(row => row.Salary), "salary");
        });

        ReportResult result = TestReport.Run(report);

        Assert.Equal([ReportPipeline.BlankKeyLabel, "apc", "VME"], result.Groups.Select(group => group.Label));
        ReportGroupResult vme = result.Groups[2];
        Assert.Equal(["Ada", "Cy", "Di"], vme.Rows.Select(row => row.Values["name"]));
        Assert.Equal([1, 2, 3], vme.Rows.Select(row => row.Number));
        Assert.Equal(
            [new ReportTotal("People", null, 3m), new ReportTotal("Salaries", "salary", 270_000m)],
            vme.Subtotals);
    }

    [Fact]
    public void PerGroupCharts_SkipSmallGroups()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.GroupBy(row => row.Department);
            report.Chart("Ages", ReportChartKind.Bar, row => row.Age >= 60 ? "60+" : "Under 60", report.Count())
                .PerGroup(minimumRows: 2)
                .CategoryOrder("Under 60", "60+");
        });

        ReportResult result = TestReport.Run(report);

        Assert.Empty(result.Charts);
        Assert.Empty(result.Groups[0].Charts);
        ReportChartResult vmeChart = Assert.Single(result.Groups[2].Charts);
        Assert.Equal(
            [new ReportChartPoint("Under 60", 1m), new ReportChartPoint("60+", 2m)],
            vmeChart.Points);
    }

    [Fact]
    public void Charts_CountOnlyTheRowsTheyInclude()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.GroupBy(row => row.Department);
            report.Chart("Ages", ReportChartKind.Bar, row => row.Age >= 60 ? "60+" : "Under 60", report.Count())
                .Where(row => row.Age < 60);
            report.Chart("Department ages", ReportChartKind.Bar, row => row.Age >= 60 ? "60+" : "Under 60", report.Count())
                .PerGroup(minimumRows: 2)
                .Where(row => row.Age < 60);
        });

        ReportResult result = TestReport.Run(report);

        // Ben, Cy and Ed are under 60; Ada and Di are left out of the chart but stay in the table.
        Assert.Equal([new ReportChartPoint("Under 60", 3m)], Assert.Single(result.Charts).Points);
        Assert.Equal(5, result.RowCount);
        // VME has three rows but only Cy is charted, which is under the per-group minimum of two.
        Assert.All(result.Groups, group => Assert.Empty(group.Charts));
    }

    #endregion

    #region Pivots and charts

    [Fact]
    public void Pivot_ComputesCellsRowTotalsColumnTotalsAndGrandTotal()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.Pivot(
                "Age band by department",
                row => row.Age >= 60 ? "60+" : "Under 60",
                row => row.Department,
                report.Count());
        });

        ReportPivotResult pivot = Assert.Single(TestReport.Run(report).Pivots);

        Assert.Equal("Age band by department", pivot.Title);
        Assert.Equal([ReportPipeline.BlankKeyLabel, "apc", "VME"], pivot.ColumnKeys);
        Assert.Equal(["60+", "Under 60"], pivot.Rows.Select(row => row.Key));
        Assert.Equal([0m, 0m, 2m], pivot.Rows[0].Values);
        Assert.Equal([1m, 1m, 1m], pivot.Rows[1].Values);
        Assert.Equal([2m, 3m], pivot.Rows.Select(row => row.Total));
        Assert.Equal([1m, 1m, 3m], pivot.ColumnTotals);
        Assert.Equal(5m, pivot.GrandTotal);
    }

    [Fact]
    public void PivotEach_CountsEverySelection()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.PivotEach("Selections", row => row.Ethnicities ?? [], row => row.Department, report.Count());
            report.PivotEach("People", row => row.Ethnicities ?? [], row => row.Department, report.CountDistinct(row => row.Name));
        });

        IReadOnlyList<ReportPivotResult> pivots = TestReport.Run(report).Pivots;

        ReportPivotRow white = pivots[0].Rows.Single(row => row.Key == "White");
        Assert.Equal([1m, 1m, 1m], white.Values);
        Assert.Equal(5m, pivots[0].GrandTotal);
        Assert.Equal(4m, pivots[1].GrandTotal);
    }

    [Fact]
    public void Chart_OrdersPreferredCategoriesFirst()
    {
        var report = new TestReport(report =>
        {
            BasicColumns(report);
            report.Chart("Departments", ReportChartKind.Pie, row => row.Department, report.Count())
                .CategoryOrder("VME", "Missing");
        });

        ReportChartResult chart = Assert.Single(TestReport.Run(report).Charts);

        Assert.Equal("Departments", chart.Title);
        Assert.Equal(ReportChartKind.Pie, chart.Kind);
        Assert.Equal(["VME", ReportPipeline.BlankKeyLabel, "apc"], chart.Points.Select(point => point.Category));
        Assert.Equal(3m, chart.Points[0].Value);
    }

    #endregion

    #region Helpers and guards

    [Theory]
    [InlineData(null, ReportPipeline.BlankKeyLabel)]
    [InlineData("  ", ReportPipeline.BlankKeyLabel)]
    [InlineData(" VME ", "VME")]
    public void NormalizeKey_TrimsAndLabelsBlanks(string? key, string expected)
    {
        Assert.Equal(expected, ReportPipeline.NormalizeKey(key));
    }

    [Fact]
    public void OrderCategories_PreferredThenAlphabetical()
    {
        Assert.Equal(
            ["c", "a", "B", "d"],
            ReportPipeline.OrderCategories(["d", "B", "a", "c"], ["c", "x"]));
    }

    [Fact]
    public void CreateResult_NullArguments_Throw()
    {
        var report = new TestReport(BasicColumns);

        Assert.Throws<ArgumentNullException>(
            () => report.CreateResult(null!, TestReport.ContextWith(), TestReport.GeneratedAt));
        Assert.Throws<ArgumentNullException>(
            () => report.CreateResult(TestReport.Rows(), null!, TestReport.GeneratedAt));
    }

    #endregion
}
