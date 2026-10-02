using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Areas.Reports.Engine;

namespace Viper.Areas.Personnel.Reports;

public sealed record EmployeesOnLeaveParameters
{
    public string? EmployeeType { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }
}

/// <summary>
/// Faculty and staff with a leave of absence in a date range, which defaults to the current
/// fiscal year. Ports the legacy "Employee on Leave" page (leaveDetective).
/// </summary>
public sealed class EmployeesOnLeaveReport : ReportDefinition<EmployeeOnLeaveRow, EmployeesOnLeaveParameters>
{
    private readonly IPersonnelReportDataService _data;

    // The clock is injected so tests can pin the fiscal-year defaults.
    private readonly TimeProvider _clock;

    public EmployeesOnLeaveReport(IPersonnelReportDataService data, TimeProvider clock)
    {
        _data = data;
        _clock = clock;
    }

    public override string Key => "personnel.employees-on-leave";

    public override string Title => "Employees on Leave";

    public override string Area => PersonnelReports.Area;

    public override string Description => "Faculty and staff on leave during a date range.";

    public override IReadOnlyList<string> Permissions => [PersonnelPermissions.Sabbatic];

    protected override void Configure(ReportBuilder<EmployeeOnLeaveRow, EmployeesOnLeaveParameters> report)
    {
        report.Choice(p => p.EmployeeType, "Employee type", PersonnelOptions.FacultyStaffBoth)
            .Required()
            .Default(() => PersonnelOptions.Faculty);
        report.Date(p => p.StartDate, "Start date").Required().Default(() => FiscalYear.StartOf(Today()));
        report.Date(p => p.EndDate, "End date").Required().Default(() => FiscalYear.EndOf(Today()));
        report.Validate(p => p.EndDate >= p.StartDate, "End date must be on or after the start date.", "endDate");
        report.Sensitive();

        report.Column(r => r.Name, "Name");
        report.Column(r => r.HomeDepartment, "Home department");
        report.Column(r => r.LeaveStart, "Leave start").AsDate();
        report.Column(r => r.ReturnDate, "Return date").AsDate();
        report.Column(r => r.Description, "Description");
        report.Column(r => r.PayStatus, "Pay status");
    }

    protected override Task<IReadOnlyList<EmployeeOnLeaveRow>> FetchAsync(
        EmployeesOnLeaveParameters parameters, ReportContext context, CancellationToken ct)
    {
        // The engine has applied defaults and required checks, so all three values are set.
        if (parameters is not { EmployeeType: { } type, StartDate: { } start, EndDate: { } end })
        {
            throw new InvalidOperationException("Employees on Leave ran without its required parameters.");
        }

        return _data.GetPeopleOnLeaveAsync(type, start, end, ct);
    }

    private DateOnly Today()
    {
        return DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
    }
}
