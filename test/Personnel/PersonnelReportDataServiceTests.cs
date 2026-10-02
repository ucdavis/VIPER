using System.Data;
using Viper.Areas.Personnel;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Areas.Reports.Data;

namespace Viper.test.Personnel;

public sealed class PersonnelReportDataServiceTests
{
    /// <summary>
    /// Records the call and maps the rows it is given, standing in for SQL Server.
    /// </summary>
    private sealed class RecordingRunner(DataTable rows) : IStoredProcedureRunner
    {
        public string? ConnectionName { get; private set; }

        public string? Procedure { get; private set; }

        public IReadOnlyList<StoredProcedureParameter> Parameters { get; private set; } = [];

        public Task<IReadOnlyList<T>> QueryAsync<T>(
            string connectionStringName,
            string procedure,
            IReadOnlyList<StoredProcedureParameter> parameters,
            Func<IDataRecord, T> map,
            CancellationToken ct)
        {
            ConnectionName = connectionStringName;
            Procedure = procedure;
            Parameters = parameters;
            using DataTableReader reader = rows.CreateDataReader();
            var mapped = new List<T>();
            while (reader.Read())
            {
                mapped.Add(map(reader));
            }

            return Task.FromResult<IReadOnlyList<T>>(mapped);
        }
    }

    private static DataTable LeaveTable()
    {
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("homeDept", typeof(string));
        table.Columns.Add("loa_begin_date", typeof(DateTime));
        table.Columns.Add("loa_return_date", typeof(DateTime));
        table.Columns.Add("loa_description", typeof(string));
        table.Columns.Add("pay_status", typeof(string));
        table.Rows.Add(
            "Lovelace, Ada   ",
            "VME  ",
            new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Local),
            "Sabbatical ",
            "With pay");
        table.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        return table;
    }

    [Fact]
    public async Task GetPeopleOnLeaveAsync_CallsTheLegacyProcedureOnPps()
    {
        var runner = new RecordingRunner(LeaveTable());
        var service = new PersonnelReportDataService(runner);

        await service.GetPeopleOnLeaveAsync(
            "S", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), TestContext.Current.CancellationToken);

        Assert.Equal(PersonnelDatabases.Pps, runner.ConnectionName);
        Assert.Equal("usp_get_peopleOnLeave", runner.Procedure);
        Assert.Equal(
            [
                StoredProcedureParameter.Text("fYear", null, 4),
                StoredProcedureParameter.Text("employeeType", "S", 1),
                StoredProcedureParameter.Date("startDate", new DateOnly(2026, 7, 1)),
                StoredProcedureParameter.Date("endDate", new DateOnly(2027, 6, 30)),
            ],
            runner.Parameters);
    }

    [Fact]
    public async Task GetPeopleOnLeaveAsync_MapsAndTrimsEachRow()
    {
        var service = new PersonnelReportDataService(new RecordingRunner(LeaveTable()));

        IReadOnlyList<EmployeeOnLeaveRow> rows = await service.GetPeopleOnLeaveAsync(
            "F", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                new EmployeeOnLeaveRow(
                    "Lovelace, Ada", "VME", new DateOnly(2026, 7, 1), new DateOnly(2027, 6, 30), "Sabbatical", "With pay"),
                new EmployeeOnLeaveRow(string.Empty, null, null, null, null, null),
            ],
            rows);
    }

    [Fact]
    public async Task GetFlsaAsync_CallsTheLegacyProcedureAndTrimsEachRow()
    {
        var table = new DataTable();
        foreach (string column in new[] { "employee_id", "emp_name", "department", "title", "flsa", "email" })
        {
            table.Columns.Add(column, typeof(string));
        }

        table.Rows.Add("000123456 ", "Lovelace, Ada ", "VME  ", "Professor ", "EXEMPT    ", "ada@ucdavis.edu");
        table.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
        var runner = new RecordingRunner(table);
        var service = new PersonnelReportDataService(runner);

        IReadOnlyList<FlsaEmployeeRow> rows = await service.GetFlsaAsync("E", TestContext.Current.CancellationToken);

        Assert.Equal(PersonnelDatabases.Pps, runner.ConnectionName);
        Assert.Equal("usp_get_flsa", runner.Procedure);
        Assert.Equal([StoredProcedureParameter.Text("statusType", "E", 1)], runner.Parameters);
        Assert.Equal(
            [
                new FlsaEmployeeRow("000123456", "Lovelace, Ada", "VME", "Professor", "EXEMPT", "ada@ucdavis.edu"),
                new FlsaEmployeeRow(null, string.Empty, null, null, null, null),
            ],
            rows);
    }

    [Fact]
    public async Task GetFacultyAppointmentsAsync_AsksForEveryEmployeeInOneCall()
    {
        var table = new DataTable();
        foreach (string column in new[] { "EMPLOYEE_ID", "EMP_NAME", "APPT_DEPT", "TITLE", "HIRE_DATE" })
        {
            table.Columns.Add(column, typeof(string));
        }

        table.Columns.Add("AGE", typeof(decimal));
        table.Rows.Add("000123456", "Lovelace, Ada", "VME", "PROF-AY ", "07/01/1995", 61m);
        table.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, string.Empty, DBNull.Value);
        var runner = new RecordingRunner(table);
        var service = new PersonnelReportDataService(runner);

        IReadOnlyList<FacultyAppointmentRow> rows =
            await service.GetFacultyAppointmentsAsync("B", TestContext.Current.CancellationToken);

        Assert.Equal("usp_get_facultyList", runner.Procedure);
        Assert.Equal(
            [
                StoredProcedureParameter.Text("facultyType", "B", 1),
                StoredProcedureParameter.Text("deptCode", string.Empty, 6),
                StoredProcedureParameter.Text("employeeID", "All", 11),
            ],
            runner.Parameters);
        Assert.Equal(
            [
                new FacultyAppointmentRow("000123456", "Lovelace, Ada", 61, new DateOnly(1995, 7, 1), "VME", "PROF-AY"),
                new FacultyAppointmentRow(string.Empty, string.Empty, null, null, null, null),
            ],
            rows);
    }
}
