using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportRunTests
{
    [Fact]
    public async Task RunAsync_FetchesWithParametersAndContextThenShapesRows()
    {
        var parameters = new SampleParams { FacultyType = "F" };
        var report = new TestReport(fetch: p => [.. TestReport.Rows().Where(row => p.FacultyType == "F" && row.Age > 60)]);
        ReportContext context = TestReport.ContextWith(TestReport.RunPermission);

        ReportResult result = await report.RunAsync(parameters, context, TestReport.GeneratedAt, TestContext.Current.CancellationToken);

        Assert.Same(parameters, report.FetchedWith);
        Assert.Same(context, report.FetchedFor);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(TestReport.GeneratedAt, result.GeneratedAt);
    }

    [Fact]
    public async Task RunAsync_WrongParameterType_Throws()
    {
        var report = new TestReport();

        await Assert.ThrowsAsync<ArgumentException>(() => report.RunAsync(
            "parameters", TestReport.ContextWith(), TestReport.GeneratedAt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RunAsync_NullContext_Throws()
    {
        var report = new TestReport();

        await Assert.ThrowsAsync<ArgumentNullException>(() => report.RunAsync(
            new SampleParams(), null!, TestReport.GeneratedAt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ParametersType_IsTheDeclaredParameterClass()
    {
        Assert.Equal(typeof(SampleParams), new TestReport().ParametersType);
    }
}
