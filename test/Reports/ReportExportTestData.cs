using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Exporters;

namespace Viper.test.Reports;

/// <summary>
/// Report results shaped for exporter tests, built through the real pipeline where possible.
/// </summary>
internal static class ReportExportTestData
{
    public static readonly IReadOnlyList<ReportFilter> Filters = [new("Faculty type", "Senate"), new("Start date", "07/01/2026")];

    /// <summary>
    /// Grouped by department with row numbers, subtotals, totals, a summary, flags, a pivot and
    /// per-group and whole-report charts.
    /// </summary>
    public static ReportResult Full()
    {
        var report = new TestReport(report =>
        {
            report.Sensitive();
            report.RowNumbers();
            report.Column(row => row.Name, "Name");
            report.Column(row => row.Age, "Age").AsNumber();
            report.Column(row => row.Salary, "Salary").AsCurrency();
            report.Column(row => row.HireDate, "Hire date").AsDate();
            report.Column(row => row.Ethnicities, "Ethnicities").AsList();
            report.Summary("People", report.Count());
            report.GroupBy(row => row.Department).Subtotal("Salaries", report.Sum(row => row.Salary), "salary");
            report.Total("All salaries", report.Sum(row => row.Salary), "salary");
            report.Highlight(row => row.Age >= 65, ReportTone.Muted, "Retirement age");
            report.Highlight(row => row.Age >= 60, ReportTone.Warning, "60+", "age");
            report.Highlight(row => row.Age < 30, ReportTone.Neutral, "Under 30");
            report.Badge(row => row.Salary is null, ReportTone.Info, "No salary", "salary");
            report.Pivot("Sample report", row => row.Age >= 60 ? "60+" : "Under 60", row => row.Department, report.Count());
            report.Chart("Ages", ReportChartKind.Bar, row => row.Age >= 60 ? "60+" : "Under 60", report.Count());
            report.Chart("Department ages", ReportChartKind.Bar, row => row.Age >= 60 ? "60+" : "Under 60", report.Count())
                .PerGroup(minimumRows: 2);
        });
        return TestReport.Run(report);
    }

    /// <summary>
    /// Ungrouped, no row numbers, no flags or totals.
    /// </summary>
    public static ReportResult Plain()
    {
        var report = new TestReport(report =>
        {
            report.Column(row => row.Name, "Name");
            report.Column(row => row.Department, "Department");
            report.Column(row => row.Salary, "Salary").AsCurrency();
        });
        return TestReport.Run(report);
    }

    /// <summary>
    /// A hand-built result covering cases the pipeline never produces, so exporters handle any
    /// well-formed result: unknown column keys, null totals, empty groups and text in numeric columns.
    /// </summary>
    public static ReportResult EdgeCases()
    {
        ReportColumnMetadata[] columns =
        [
            new("name", "Name", ReportColumnFormat.Text, ReportAlignment.Left, null),
            new("when", "When", ReportColumnFormat.Date, ReportAlignment.Center, null),
            new("share", "Share", ReportColumnFormat.Percent, ReportAlignment.Right, 1),
            new("amount", "Amount", ReportColumnFormat.Number, ReportAlignment.Right, null),
        ];
        var row = new ReportRowResult(
            1,
            new Dictionary<string, object?>
            {
                ["name"] = "=cmd|' /C calc'!A0",
                ["when"] = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Local),
                ["share"] = 0.125m,
                ["amount"] = "pending",
            },
            [new ReportFlag(ReportFlagKind.Highlight, ReportTone.Negative, "Check", "missing")]);
        return new ReportResult(
            "test.edge",
            "Edge cases",
            TestReport.GeneratedAt,
            1,
            false,
            columns,
            [],
            [new ReportGroupResult(null, [row], [], []), new ReportGroupResult(null, [], [], [])],
            [new ReportTotal("Unplaced", "missing", 3m), new ReportTotal("Unknown", null, null)],
            [],
            []);
    }

    public static ReportExport Export(ReportResult result, bool confidential = false, IReadOnlyList<ReportFilter>? filters = null)
    {
        return new ReportExport(result, filters ?? [], confidential);
    }
}
