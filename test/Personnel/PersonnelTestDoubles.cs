using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;

namespace Viper.test.Personnel;

/// <summary>
/// Returns canned rows and records what each report asked for.
/// </summary>
internal sealed class StubPersonnelReportDataService : IPersonnelReportDataService
{
    public IReadOnlyList<EmployeeOnLeaveRow> PeopleOnLeave { get; init; } = [];

    public (string Type, DateOnly Start, DateOnly End)? LeaveRequest { get; private set; }

    public IReadOnlyList<FlsaEmployeeRow> FlsaEmployees { get; init; } = [];

    public string? FlsaRequest { get; private set; }

    public IReadOnlyList<FacultyAppointmentRow> FacultyAppointments { get; init; } = [];

    public string? FacultyRequest { get; private set; }

    public Task<IReadOnlyList<EmployeeOnLeaveRow>> GetPeopleOnLeaveAsync(
        string employeeType, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        LeaveRequest = (employeeType, startDate, endDate);
        return Task.FromResult(PeopleOnLeave);
    }

    public Task<IReadOnlyList<FlsaEmployeeRow>> GetFlsaAsync(string statusType, CancellationToken ct)
    {
        FlsaRequest = statusType;
        return Task.FromResult(FlsaEmployees);
    }

    public Task<IReadOnlyList<FacultyAppointmentRow>> GetFacultyAppointmentsAsync(string facultyType, CancellationToken ct)
    {
        FacultyRequest = facultyType;
        return Task.FromResult(FacultyAppointments);
    }
}
