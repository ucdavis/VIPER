using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Reports;
using Viper.Areas.Reports.Engine;

namespace Viper.test.Personnel;

public sealed class ExemptStatusReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 9, 0, 0, TimeSpan.Zero);

    // Ada holds two exempt appointments in VME, so VME counts her once.
    private readonly StubPersonnelReportDataService _data = new()
    {
        FlsaEmployees =
        [
            new FlsaEmployeeRow("1", "Lovelace, Ada", "VME", "Professor", ExemptStatusReport.Exempt, "ada@ucdavis.edu"),
            new FlsaEmployeeRow("1", "Lovelace, Ada", "VME", "Chair", ExemptStatusReport.Exempt, "ada@ucdavis.edu"),
            new FlsaEmployeeRow("2", "Byron, Bo", "VME", "Analyst", ExemptStatusReport.NonExempt, null),
            new FlsaEmployeeRow("3", "Curie, Cy", "APC", "Assistant", ExemptStatusReport.NonExempt, null),
        ],
    };

    private ExemptStatusReport Report()
    {
        return new ExemptStatusReport(_data);
    }

    private static ReportContext Context()
    {
        return new ReportContext("tester", permission => permission == PersonnelPermissions.ServiceCreditAdmin);
    }

    private async Task<ReportResult> RunAsync(string statusType)
    {
        ExemptStatusReport report = Report();
        var parameters = new ExemptStatusParameters { StatusType = statusType };
        Assert.Empty(report.PrepareParameters(parameters));
        return await report.RunAsync(parameters, Context(), Now, TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Identity_UsesTheDecidedPermission()
    {
        ExemptStatusReport report = Report();

        Assert.Equal("personnel.exempt-non-exempt", report.Key);
        Assert.Equal([PersonnelPermissions.ServiceCreditAdmin], report.Permissions);
        Assert.False(report.IsSensitive);
        Assert.True(report.CanRun(Context()));
    }

    [Fact]
    public void Metadata_DefaultsToBoth()
    {
        ReportDefinitionMetadata metadata = Report().GetMetadata(Context());

        ReportParameterMetadata status = Assert.Single(metadata.Parameters);
        Assert.Equal("statusType", status.Name);
        Assert.Equal("B", status.DefaultValue);
        Assert.Equal(["E", "N", "B"], status.Options.Select(option => option.Value));
        Assert.Equal(["name", "department", "title", "flsa", "email"], metadata.Columns.Select(column => column.Key));
    }

    [Fact]
    public async Task RunAsync_AsksForTheChosenStatus()
    {
        await RunAsync("E");

        Assert.Equal("E", _data.FlsaRequest);
    }

    [Fact]
    public async Task RunAsync_CountsPeopleForTheSchool()
    {
        ReportResult result = await RunAsync("B");

        Assert.Equal(
            [new ReportTotal("Exempt employees", null, 1), new ReportTotal("Non-exempt employees", null, 2)],
            result.Summary);
        Assert.True(result.RowNumbers);
    }

    [Fact]
    public async Task RunAsync_GroupsByDepartmentWithNonExemptFirst()
    {
        ReportResult result = await RunAsync("B");

        Assert.Equal(["APC", "VME"], result.Groups.Select(group => group.Label));
        ReportGroupResult vme = result.Groups[1];
        Assert.Equal(
            ["Byron, Bo", "Lovelace, Ada", "Lovelace, Ada"],
            vme.Rows.Select(row => row.Values["name"]));
        Assert.Equal(
            [new ReportTotal("Exempt employees", null, 1), new ReportTotal("Non-exempt employees", null, 1)],
            vme.Subtotals);
    }

    [Fact]
    public async Task RunAsync_WithoutPreparedParameters_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Report().RunAsync(
            new ExemptStatusParameters(), Context(), Now, TestContext.Current.CancellationToken));
    }
}
