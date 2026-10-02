using Viper.Areas.Reports.Engine;

namespace Viper.test.Reports;

public sealed class ReportDescribeParametersTests
{
    [Fact]
    public void DescribeParameters_UsesLabelsAndReadableValues()
    {
        var report = new TestReport();
        var parameters = new SampleParams
        {
            FacultyType = "F",
            Departments = ["VME", "APC"],
            StartDate = new DateOnly(2025, 7, 1),
            Search = "smith",
            MinimumAge = 60,
            IncludeEmeriti = true,
        };

        IReadOnlyList<ReportFilter> filters = report.DescribeParameters(parameters);

        Assert.Equal(
            [
                new ReportFilter("Faculty type", "Federation"),
                new ReportFilter("Departments", "Medicine and Epidemiology, Anatomy, Physiology and Cell Biology"),
                new ReportFilter("Start date", "07/01/2025"),
                new ReportFilter("Search", "smith"),
                new ReportFilter("Minimum age", "60"),
                new ReportFilter("Include emeriti", "Yes"),
            ],
            filters);
    }

    [Fact]
    public void DescribeParameters_SkipsEmptyValuesAndShowsNo()
    {
        var report = new TestReport();

        IReadOnlyList<ReportFilter> filters = report.DescribeParameters(new SampleParams { IncludeEmeriti = false, Search = " " });

        Assert.Equal([new ReportFilter("Include emeriti", "No")], filters);
    }

    [Fact]
    public void DescribeParameters_UnknownChoice_ShowsTheRawValue()
    {
        var report = new TestReport();

        IReadOnlyList<ReportFilter> filters = report.DescribeParameters(new SampleParams { FacultyType = "Z" });

        Assert.Equal([new ReportFilter("Faculty type", "Z")], filters);
    }

    [Fact]
    public void DescribeParameters_WrongType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TestReport().DescribeParameters("parameters"));
    }

    [Fact]
    public void IsSensitive_FollowsTheLayout()
    {
        var sensitive = new TestReport(report =>
        {
            report.Sensitive();
            report.Column(row => row.Name, "Name");
        });

        Assert.False(new TestReport().IsSensitive);
        Assert.True(sensitive.IsSensitive);
    }
}
