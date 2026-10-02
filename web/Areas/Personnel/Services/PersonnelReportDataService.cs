using System.Data;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Reports.Data;

namespace Viper.Areas.Personnel.Services;

/// <summary>
/// Reads the data behind the Personnel reports from the PPS warehouse.
/// </summary>
public interface IPersonnelReportDataService
{
    /// <summary>
    /// Leaves of absence overlapping the date range, for faculty ("F"), staff ("S") or both ("B").
    /// </summary>
    Task<IReadOnlyList<EmployeeOnLeaveRow>> GetPeopleOnLeaveAsync(
        string employeeType, DateOnly startDate, DateOnly endDate, CancellationToken ct);

    /// <summary>
    /// Employees by FLSA status: exempt ("E"), non-exempt ("N") or both ("B").
    /// </summary>
    Task<IReadOnlyList<FlsaEmployeeRow>> GetFlsaAsync(string statusType, CancellationToken ct);

    /// <summary>
    /// Every paid appointment of Senate ("S"), Federation ("F") or both ("B") faculty in the school.
    /// </summary>
    Task<IReadOnlyList<FacultyAppointmentRow>> GetFacultyAppointmentsAsync(string facultyType, CancellationToken ct);
}

/// <summary>
/// Calls the legacy PPS stored procedures, so results match the ColdFusion reports while they
/// run side by side. Each method only calls and maps; report rules live in the report definitions.
/// </summary>
public class PersonnelReportDataService : IPersonnelReportDataService
{
    private const string PeopleOnLeaveProcedure = "usp_get_peopleOnLeave";
    private const string FlsaProcedure = "usp_get_flsa";
    private const string FacultyListProcedure = "usp_get_facultyList";

    // usp_get_facultyList returns every employee's appointments when @employeeID is "All".
    private const string AllEmployees = "All";
    private const string LegacyDateFormat = "MM/dd/yyyy";

    private readonly IStoredProcedureRunner _runner;

    public PersonnelReportDataService(IStoredProcedureRunner runner)
    {
        _runner = runner;
    }

    public Task<IReadOnlyList<EmployeeOnLeaveRow>> GetPeopleOnLeaveAsync(
        string employeeType, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        // @fYear is the procedure's older fiscal-year filter; the date range replaces it.
        return _runner.QueryAsync(
            PersonnelDatabases.Pps,
            PeopleOnLeaveProcedure,
            [
                StoredProcedureParameter.Text("fYear", null, 4),
                StoredProcedureParameter.Text("employeeType", employeeType, 1),
                StoredProcedureParameter.Date("startDate", startDate),
                StoredProcedureParameter.Date("endDate", endDate),
            ],
            MapEmployeeOnLeave,
            ct);
    }

    public Task<IReadOnlyList<FlsaEmployeeRow>> GetFlsaAsync(string statusType, CancellationToken ct)
    {
        return _runner.QueryAsync(
            PersonnelDatabases.Pps,
            FlsaProcedure,
            [StoredProcedureParameter.Text("statusType", statusType, 1)],
            MapFlsaEmployee,
            ct);
    }

    public Task<IReadOnlyList<FacultyAppointmentRow>> GetFacultyAppointmentsAsync(string facultyType, CancellationToken ct)
    {
        return _runner.QueryAsync(
            PersonnelDatabases.Pps,
            FacultyListProcedure,
            [
                StoredProcedureParameter.Text("facultyType", facultyType, 1),
                StoredProcedureParameter.Text("deptCode", string.Empty, 6),
                StoredProcedureParameter.Text("employeeID", AllEmployees, 11),
            ],
            MapFacultyAppointment,
            ct);
    }

    // Stored procedure rows are mapped by hand: Mapperly maps between types, not from IDataRecord.
    internal static EmployeeOnLeaveRow MapEmployeeOnLeave(IDataRecord record)
    {
        return new EmployeeOnLeaveRow(
            record.GetTrimmedString("Name") ?? string.Empty,
            record.GetTrimmedString("homeDept"),
            record.GetDateOnly("loa_begin_date"),
            record.GetDateOnly("loa_return_date"),
            record.GetTrimmedString("loa_description"),
            record.GetTrimmedString("pay_status"));
    }

    internal static FlsaEmployeeRow MapFlsaEmployee(IDataRecord record)
    {
        return new FlsaEmployeeRow(
            record.GetTrimmedString("employee_id"),
            record.GetTrimmedString("emp_name") ?? string.Empty,
            record.GetTrimmedString("department"),
            record.GetTrimmedString("title"),
            record.GetTrimmedString("flsa"),
            record.GetTrimmedString("email"));
    }

    internal static FacultyAppointmentRow MapFacultyAppointment(IDataRecord record)
    {
        return new FacultyAppointmentRow(
            record.GetTrimmedString("EMPLOYEE_ID") ?? string.Empty,
            record.GetTrimmedString("EMP_NAME") ?? string.Empty,
            record.GetNullableInt32("AGE"),
            record.GetDateOnlyFromText("HIRE_DATE", LegacyDateFormat),
            record.GetTrimmedString("APPT_DEPT"),
            record.GetTrimmedString("TITLE"));
    }
}
