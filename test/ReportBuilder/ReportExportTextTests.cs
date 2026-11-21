using Viper.Areas.ReportBuilder.Exporters;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.test.ReportBuilder;

public sealed class ReportExportTextTests
{
    [Fact]
    public void Generated_IsUsDateAndTime()
    {
        Assert.Equal("Generated 11/16/2026 9:05 AM", ReportExportText.Generated(new DateTimeOffset(2026, 11, 16, 9, 5, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Filters_JoinsLabelsAndValues()
    {
        Assert.Equal(
            "Filters: Department: Pathology; From: 07/01/2026",
            ReportExportText.Filters(
            [
                new ReportAppliedFilter { Label = "Department", Value = "Pathology" },
                new ReportAppliedFilter { Label = "From", Value = "07/01/2026" },
            ]));
    }

    [Fact]
    public void Filters_None_IsNull()
    {
        Assert.Null(ReportExportText.Filters([]));
    }

    [Fact]
    public void Truncated_NamesTheCap()
    {
        Assert.Equal("Only the first 50,000 rows are included.", ReportExportText.Truncated());
    }
}
