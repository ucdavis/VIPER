using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Reports;
using Viper.Areas.Reports.Engine;
using Viper.test.Reports;

namespace Viper.test.Personnel;

public sealed class EmployeesOnLeaveReportTests
{
    // A February date, so the default range is the fiscal year that began the previous July.
    private static readonly DateTimeOffset Now = new(2027, 2, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly StubPersonnelReportDataService _data = new()
    {
        PeopleOnLeave =
        [
            new EmployeeOnLeaveRow(
                "Lovelace, Ada", "VME", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "Sabbatical", "With pay"),
        ],
    };

    private EmployeesOnLeaveReport Report()
    {
        return new EmployeesOnLeaveReport(_data, new FixedTimeProvider(Now));
    }

    private static ReportContext Context()
    {
        return new ReportContext("tester", permission => permission == PersonnelPermissions.Sabbatic);
    }

    [Fact]
    public void Identity_MatchesTheLegacyPage()
    {
        EmployeesOnLeaveReport report = Report();

        Assert.Equal("personnel.employees-on-leave", report.Key);
        Assert.Equal("Personnel", report.Area);
        Assert.Equal([PersonnelPermissions.Sabbatic], report.Permissions);
        Assert.True(report.IsSensitive);
        Assert.True(report.CanRun(Context()));
    }

    [Fact]
    public void Metadata_DefaultsToFacultyAndTheCurrentFiscalYear()
    {
        ReportDefinitionMetadata metadata = Report().GetMetadata(Context());

        Assert.Equal(
            [("employeeType", (object?)"F"), ("startDate", new DateOnly(2026, 7, 1)), ("endDate", new DateOnly(2027, 6, 30))],
            metadata.Parameters.Select(parameter => (parameter.Name, parameter.DefaultValue)));
        Assert.All(metadata.Parameters, parameter => Assert.True(parameter.Required));
        Assert.Equal(
            ["name", "homeDepartment", "leaveStart", "returnDate", "description", "payStatus"],
            metadata.Columns.Select(column => column.Key));
    }

    [Fact]
    public void PrepareParameters_EndBeforeStart_IsInvalid()
    {
        var parameters = new EmployeesOnLeaveParameters
        {
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 6, 30),
        };

        ReportValidationError error = Assert.Single(Report().PrepareParameters(parameters));

        Assert.Equal("endDate", error.Parameter);
    }

    [Fact]
    public async Task RunAsync_PassesThePreparedParametersToTheDataService()
    {
        EmployeesOnLeaveReport report = Report();
        var parameters = new EmployeesOnLeaveParameters { EmployeeType = "B", StartDate = new DateOnly(2025, 1, 1) };
        Assert.Empty(report.PrepareParameters(parameters));

        ReportResult result = await report.RunAsync(parameters, Context(), Now, TestContext.Current.CancellationToken);

        Assert.Equal(("B", new DateOnly(2025, 1, 1), new DateOnly(2027, 6, 30)), _data.LeaveRequest);
        Assert.Equal(1, result.RowCount);
        Assert.Equal("Lovelace, Ada", Assert.Single(Assert.Single(result.Groups).Rows).Values["name"]);
    }

    [Fact]
    public async Task RunAsync_WithoutPreparedParameters_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Report().RunAsync(
            new EmployeesOnLeaveParameters(), Context(), Now, TestContext.Current.CancellationToken));
    }
}
