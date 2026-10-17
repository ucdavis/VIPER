using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models.Eis;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Personnel.Services;

/// <summary>
/// Reads EIS data: the legacy AcademicPersonnel procedures, the PPS job and stipend views, the
/// MyInfoVault procedures in AcademicPersonnel and MPVote, and the employee's AAUD identifiers. Only calls and returns rows; <see cref="EisService"/> interprets them.
/// </summary>
public interface IEisDataService
{
    /// <summary>Every employee EIS can show, as name and employee ID.</summary>
    Task<IReadOnlyList<EisPersonRow>> GetPeopleAsync(CancellationToken ct);

    /// <summary>The employees paid by the units listed for <paramref name="loginId"/> in psaDepartmentList.</summary>
    Task<IReadOnlyList<EisPersonRow>> GetUnitPeopleAsync(string loginId, CancellationToken ct);

    /// <summary>The employee's MothraID, mail ID and PPS ID from AAUD, or null when AAUD has no record.</summary>
    Task<EisPersonIds?> GetPersonIdsAsync(string employeeId, CancellationToken ct);

    Task<EisHeaderData> GetHeaderAsync(string employeeId, CancellationToken ct);

    Task<IReadOnlyList<EisAppointmentRow>> GetAppointmentsAsync(string employeeId, CancellationToken ct);

    Task<IReadOnlyList<EisDistributionRow>> GetDistributionsAsync(string employeeId, string appointmentNumber, CancellationToken ct);

    Task<IReadOnlyList<EisStipendRow>> GetStipendsAsync(string employeeId, CancellationToken ct);

    Task<IReadOnlyList<EisHistoryRow>> GetHistoryAsync(string employeeId, CancellationToken ct);

    Task<IReadOnlyList<EisPpsHistoryRow>> GetPpsHistoryAsync(string ppsId, CancellationToken ct);

    Task<IReadOnlyList<EisLeaveRow>> GetLeavesAsync(string employeeId, CancellationToken ct);

    Task<IReadOnlyList<EisLeaveRow>> GetPpsLeavesAsync(string ppsId, CancellationToken ct);

    Task<EisPermanentAddressRow?> GetPermanentAddressAsync(string employeeId, CancellationToken ct);

    Task<EisHomePhoneRow?> GetHomePhoneAsync(string employeeId, CancellationToken ct);

    /// <summary>Every job on the employee's record, with the tests the category procedures made.</summary>
    Task<IReadOnlyList<EisJobRow>> GetJobsAsync(string employeeId, CancellationToken ct);

    /// <summary>The compensation rate codes on the employee's active jobs.</summary>
    Task<IReadOnlyList<string>> GetRateCodesAsync(string employeeId, CancellationToken ct);

    /// <summary>The manual category codes set for the employee in <paramref name="academicYear"/>.</summary>
    Task<IReadOnlyList<int>> GetFlagCodesAsync(string employeeId, string academicYear, CancellationToken ct);

    /// <summary>
    /// Sets (<paramref name="isSet"/> true) or clears a manual category for the current academic
    /// year through usp_eis_putPersonModFlags, which computes the year itself.
    /// </summary>
    Task SetFlagAsync(string employeeId, int code, bool isSet, CancellationToken ct);

    Task<EisPrograms> GetProgramsAsync(string employeeId, CancellationToken ct);

    Task<IReadOnlyList<EisMivDegreeRow>> GetMivDegreesAsync(int mivId, CancellationToken ct);

    Task<IReadOnlyList<EisMivTextRow>> GetMivMembershipsAsync(int mivId, CancellationToken ct);

    Task<IReadOnlyList<EisMivTextRow>> GetMivHonorsAsync(int mivId, CancellationToken ct);

    Task<IReadOnlyList<EisMivTextRow>> GetMivBoardsAsync(int mivId, CancellationToken ct);

    Task<IReadOnlyList<EisMivTextRow>> GetMivResearchFocusAsync(int mivId, CancellationToken ct);

    Task<IReadOnlyList<EisMivTextRow>> GetMivSpecialtyFocusAsync(int mivId, CancellationToken ct);
}

/// <summary>
/// Calls the procedures through <see cref="AcademicPersonnelContext"/>. Every value is a typed
/// SqlParameter sized like the procedure's own parameter, so no SQL is built from input. Results
/// are read with ToListAsync because EF can't compose further SQL over an EXEC.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Only issues SQL against AcademicPersonnel, MPVote and AAUD; EisService is tested against a fake.")]
public class EisDataService : IEisDataService
{
    private const int EmployeeIdLength = 11;
    private const int PpsIdLength = 9;
    private const int LoginIdLength = 18;
    private const int AppointmentNumberLength = 10;
    private const int DepartmentFieldLength = 50;
    private const string HomeDepartmentField = "HOME_DEPT";
    private const string AlternateDepartmentField = "ALT_DEPT_CD";
    private const int AcademicYearLength = 9;
    private const string AddFlag = "A";
    private const string DeleteFlag = "D";

    private readonly AcademicPersonnelContext _context;
    private readonly AAUDContext _aaudContext;
    private readonly MPVoteContext _mpVoteContext;

    public EisDataService(AcademicPersonnelContext context, AAUDContext aaudContext, MPVoteContext mpVoteContext)
    {
        _context = context;
        _aaudContext = aaudContext;
        _mpVoteContext = mpVoteContext;
    }

    public async Task<IReadOnlyList<EisPersonRow>> GetPeopleAsync(CancellationToken ct)
    {
        return await Query<EisPersonRow>($"EXEC dbo.usp_eis_getPersonnelList", ct);
    }

    public async Task<IReadOnlyList<EisPersonRow>> GetUnitPeopleAsync(string loginId, CancellationToken ct)
    {
        SqlParameter login = Text("@psaLoginID", loginId, LoginIdLength);
        return await Query<EisPersonRow>($"EXEC dbo.usp_eis_getPersonnelListUnit @psaLoginID = {login}", ct);
    }

    public async Task<EisPersonIds?> GetPersonIdsAsync(string employeeId, CancellationToken ct)
    {
        // An employee can have several AAUD rows over time; prefer the current one.
        return await _aaudContext.AaudUsers
            .AsNoTracking()
            .Where(user => user.EmployeeId == employeeId)
            .OrderByDescending(user => user.Current)
            .ThenByDescending(user => user.EmployeeTerm)
            .Select(user => new EisPersonIds(user.MothraId, user.MailId, user.PpsId, user.MivId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EisHeaderData> GetHeaderAsync(string employeeId, CancellationToken ct)
    {
        // One procedure per field, as the legacy header called them. A DbContext can't run
        // queries in parallel, so they run one after another.
        EisLeaveBalanceRow? balances = await First<EisLeaveBalanceRow>($"EXEC dbo.usp_eis_getPersonLeaveBalances @employeeID = {Id(employeeId)}", ct);
        return new EisHeaderData
        {
            Name = (await First<EisNameRow>($"EXEC dbo.usp_eis_getPersonName @employeeID = {Id(employeeId)}", ct))?.Name,
            DateOfBirth = (await First<EisDateOfBirthRow>($"EXEC dbo.usp_eis_getPersonDOB @employeeID = {Id(employeeId)}", ct))?.DateOfBirth,
            Age = (await First<EisAgeRow>($"EXEC dbo.usp_eis_getPersonAge @employeeID = {Id(employeeId)}", ct))?.Age,
            Gender = (await First<EisGenderRow>($"EXEC dbo.usp_eis_getPersonGender @employeeID = {Id(employeeId)}", ct))?.Gender,
            Ethnicity = (await First<EisEthnicityRow>($"EXEC dbo.usp_eis_getPersonEthnicity @employeeID = {Id(employeeId)}", ct))?.Ethnicity,
            HireDate = (await First<EisHireDateRow>($"EXEC dbo.usp_eis_getPersonHireDate @employeeID = {Id(employeeId)}", ct))?.HireDate,
            EmploymentStatus = (await First<EisEmploymentStatusRow>($"EXEC dbo.usp_eis_getPersonEmpStatus @employeeID = {Id(employeeId)}", ct))?.Status,
            Citizenship = (await First<EisCitizenshipRow>($"EXEC dbo.usp_eis_getPersonCitizenship @employeeID = {Id(employeeId)}", ct))?.Citizenship,
            Visa = (await First<EisVisaRow>($"EXEC dbo.usp_eis_getPersonVisa @employeeID = {Id(employeeId)}", ct))?.Description,
            PrimaryAffiliation = (await First<EisAffiliationRow>($"EXEC dbo.usp_eis_getPersonPrimaryAffiliation @employeeID = {Id(employeeId)}", ct))?.Affiliation,
            PrimaryTitle = (await First<EisTitleRow>($"EXEC dbo.usp_eis_getPersonPrimaryTitle @employeeID = {Id(employeeId)}", ct))?.Title,
            JobGroup = (await First<EisJobGroupRow>($"EXEC dbo.usp_eis_getPersonJobGroup @employeeID = {Id(employeeId)}", ct))?.JobGroup,
            HomeDepartment = await GetDepartmentAsync(employeeId, HomeDepartmentField, ct),
            AlternateDepartment = await GetDepartmentAsync(employeeId, AlternateDepartmentField, ct),
            BargainingUnit = (await First<EisBargainingUnitRow>($"EXEC dbo.usp_eis_getPersonCBUC @employeeID = {Id(employeeId)}", ct))?.BargainingUnit,
            LaborRelationsUnit = (await First<EisNamedValueRow>($"EXEC dbo.usp_eis_getPersonRelUnit @employeeID = {Id(employeeId)}", ct))?.Name,
            StaffCount = (await First<EisCountRow>($"EXEC dbo.usp_eis_isStaff @employeeID = {Id(employeeId)}", ct))?.Count,
            StaffProgram = (await First<EisNamedValueRow>($"EXEC dbo.usp_eis_getPersonStaffProgram @employeeID = {Id(employeeId)}", ct))?.Name,
            StaffStatus = (await First<EisStaffStatusRow>($"EXEC dbo.usp_eis_getPersonStaffStatus @employeeID = {Id(employeeId)}", ct))?.Status,
            FacultyCount = (await First<EisCountRow>($"EXEC dbo.usp_eis_isFaculty @employeeID = {Id(employeeId)}", ct))?.Count,
            FacultyProgram = (await First<EisNamedValueRow>($"EXEC dbo.usp_eis_getPersonFacultyProgram @employeeID = {Id(employeeId)}", ct))?.Name,
            LadderRank = (await First<EisLadderRankRow>($"EXEC dbo.usp_eis_isLadderRank @employeeID = {Id(employeeId)}", ct))?.LadderRank,
            ServiceCredit = (await First<EisServiceCreditRow>($"EXEC dbo.usp_eis_getPersonServiceCredit @employeeID = {Id(employeeId)}", ct))?.Credit,
            VacationBalance = balances?.Vacation,
            SickBalance = balances?.Sick,
            PtoBalance = balances?.Pto,
        };
    }

    public async Task<IReadOnlyList<EisAppointmentRow>> GetAppointmentsAsync(string employeeId, CancellationToken ct)
    {
        return await Query<EisAppointmentRow>($"EXEC dbo.usp_eis_getPersonAppt @employeeID = {Id(employeeId)}", ct);
    }

    public async Task<IReadOnlyList<EisDistributionRow>> GetDistributionsAsync(
        string employeeId, string appointmentNumber, CancellationToken ct)
    {
        SqlParameter id = Id(employeeId);
        SqlParameter appointment = Text("@apptNumber", appointmentNumber, AppointmentNumberLength);
        return await Query<EisDistributionRow>(
            $"EXEC dbo.usp_eis_getPersonDistRecords @employeeID = {id}, @apptNumber = {appointment}", ct);
    }

    public async Task<IReadOnlyList<EisStipendRow>> GetStipendsAsync(string employeeId, CancellationToken ct)
    {
        SqlParameter id = Id(employeeId);
        return await Query<EisStipendRow>(
            $"""
            SELECT EFFDT, EARNINGS_END_DT, ERNCD, OTH_PAY, ADDL_PAY_FREQUENCY
            FROM PPS.dbo.vw_stipends
            WHERE EMPLID = {id}
            ORDER BY EFFDT
            """,
            ct);
    }

    public async Task<IReadOnlyList<EisHistoryRow>> GetHistoryAsync(string employeeId, CancellationToken ct)
    {
        return await Query<EisHistoryRow>($"EXEC dbo.usp_eis_getPersonHistory @employeeID = {Id(employeeId)}", ct);
    }

    public async Task<IReadOnlyList<EisPpsHistoryRow>> GetPpsHistoryAsync(string ppsId, CancellationToken ct)
    {
        SqlParameter id = Text("@employeeID", ppsId, PpsIdLength);
        return await Query<EisPpsHistoryRow>($"EXEC dbo.usp_eis_getPersonPPSHistory @employeeID = {id}", ct);
    }

    public async Task<IReadOnlyList<EisLeaveRow>> GetLeavesAsync(string employeeId, CancellationToken ct)
    {
        return await Query<EisLeaveRow>($"EXEC dbo.usp_eis_getPersonLOA @employeeID = {Id(employeeId)}", ct);
    }

    public async Task<IReadOnlyList<EisLeaveRow>> GetPpsLeavesAsync(string ppsId, CancellationToken ct)
    {
        SqlParameter id = Text("@employeeID", ppsId, PpsIdLength);
        return await Query<EisLeaveRow>($"EXEC dbo.usp_eis_getPersonPPSLOA @employeeID = {id}", ct);
    }

    public async Task<EisPermanentAddressRow?> GetPermanentAddressAsync(string employeeId, CancellationToken ct)
    {
        return await First<EisPermanentAddressRow>($"EXEC dbo.usp_eis_getPersonPermanentAddress @employeeID = {Id(employeeId)}", ct);
    }

    public async Task<EisHomePhoneRow?> GetHomePhoneAsync(string employeeId, CancellationToken ct)
    {
        return await First<EisHomePhoneRow>($"EXEC dbo.usp_eis_getPersonHomePhone @employeeID = {Id(employeeId)}", ct);
    }

    public async Task<IReadOnlyList<EisJobRow>> GetJobsAsync(string employeeId, CancellationToken ct)
    {
        // The legacy category procedures each tested one condition over this view; computing
        // the conditions here lets one query answer all of them.
        SqlParameter id = Id(employeeId);
        return await Query<EisJobRow>(
            $"""
            SELECT CAST(LTRIM(RTRIM(JOBCODE)) AS varchar(10)) AS JobCode,
                CAST(LTRIM(RTRIM(JOBGROUP)) AS varchar(10)) AS JobGroup,
                CAST(CASE WHEN EFFDT <= GETDATE() AND (EXPECTED_END_DATE IS NULL OR EXPECTED_END_DATE >= GETDATE()) THEN 1 ELSE 0 END AS bit) AS IsCurrent,
                CAST(CASE WHEN ANNUAL_RT > 0 THEN 1 ELSE 0 END AS bit) AS IsPaid,
                CAST(CASE WHEN ANNUAL_RT = 0 THEN 1 ELSE 0 END AS bit) AS IsWithoutSalary,
                CAST(CASE WHEN EFF_DATE_ACTIVE = 1 THEN 1 ELSE 0 END AS bit) AS IsActive,
                CAST(JOBCODE_DESC AS varchar(100)) AS Description
            FROM PPS.dbo.vw_PersonJobPosition
            WHERE EMPLID = {id}
            """,
            ct);
    }

    public async Task<IReadOnlyList<string>> GetRateCodesAsync(string employeeId, CancellationToken ct)
    {
        SqlParameter id = Id(employeeId);
        List<EisRateCodeRow> rows = await Query<EisRateCodeRow>(
            $"""
            SELECT DISTINCT CAST(LTRIM(RTRIM(comp.COMP_RATECD)) AS varchar(10)) AS RateCode
            FROM PPS.dbo.vw_PersonJobPosition job
                INNER JOIN PPS.dbo.PS_COMPENSATION_V comp
                    ON comp.EMPLID = job.EMPLID AND comp.EMPL_RCD = job.EMPL_RCD
                    AND comp.EFFDT = job.EFFDT AND comp.EFFSEQ = job.EFFSEQ
            WHERE job.EMPLID = {id} AND job.EFF_DATE_ACTIVE = 1 AND comp.COMP_RATECD IS NOT NULL
            """,
            ct);
        return [.. rows.Select(row => row.RateCode).OfType<string>()];
    }

    public async Task<IReadOnlyList<int>> GetFlagCodesAsync(string employeeId, string academicYear, CancellationToken ct)
    {
        SqlParameter id = Id(employeeId);
        SqlParameter year = Text("@academicYear", academicYear, AcademicYearLength);
        List<EisFlagRow> rows = await Query<EisFlagRow>(
            $"""
            SELECT DISTINCT CAST(flags_code AS int) AS Code
            FROM dbo.flags
            WHERE flags_employee_id = {id} AND flags_academicYear = {year}
            """,
            ct);
        return [.. rows.Select(row => row.Code)];
    }

    public async Task SetFlagAsync(string employeeId, int code, bool isSet, CancellationToken ct)
    {
        SqlParameter id = Id(employeeId);
        SqlParameter action = Text("@action", isSet ? AddFlag : DeleteFlag, 1);
        var flag = new SqlParameter("@code", SqlDbType.SmallInt) { Value = (short)code };
        await _context.Database.ExecuteSqlAsync(
            $"EXEC dbo.usp_eis_putPersonModFlags @employeeID = {id}, @action = {action}, @code = {flag}", ct);
    }

    public async Task<EisPrograms> GetProgramsAsync(string employeeId, CancellationToken ct)
    {
        string? staff = (await First<EisNamedValueRow>($"EXEC dbo.usp_eis_getPersonStaffProgram @employeeID = {Id(employeeId)}", ct))?.Name;
        string? faculty = (await First<EisNamedValueRow>($"EXEC dbo.usp_eis_getPersonFacultyProgram @employeeID = {Id(employeeId)}", ct))?.Name;
        return new EisPrograms(staff, faculty);
    }

    public async Task<IReadOnlyList<EisMivDegreeRow>> GetMivDegreesAsync(int mivId, CancellationToken ct)
    {
        List<Dictionary<string, string?>> rows = await ReadProcedureAsync(_context, "dbo.usp_eis_getPersonMIVDegrees", mivId, ct);
        return [.. rows.Select(row => new EisMivDegreeRow(
            Field(row, "Degree"), Field(row, "StartDate"), Field(row, "EndDate"),
            Field(row, "Institution"), Field(row, "location"), Field(row, "field")))];
    }

    public async Task<IReadOnlyList<EisMivTextRow>> GetMivMembershipsAsync(int mivId, CancellationToken ct)
    {
        return TextRows(await ReadProcedureAsync(_context, "dbo.usp_eis_getPersonMIVProfesionalMemberships", mivId, ct), null, "membership");
    }

    public async Task<IReadOnlyList<EisMivTextRow>> GetMivHonorsAsync(int mivId, CancellationToken ct)
    {
        return TextRows(await ReadProcedureAsync(_context, "dbo.usp_eis_getPersonMIVHonors", mivId, ct), "Year", "Description");
    }

    public async Task<IReadOnlyList<EisMivTextRow>> GetMivBoardsAsync(int mivId, CancellationToken ct)
    {
        return TextRows(await ReadProcedureAsync(_mpVoteContext, "dbo.usp_get_boards", mivId, ct), "YearSpan", "Content");
    }

    public async Task<IReadOnlyList<EisMivTextRow>> GetMivResearchFocusAsync(int mivId, CancellationToken ct)
    {
        return TextRows(await ReadProcedureAsync(_mpVoteContext, "dbo.usp_get_researchFocus", mivId, ct), null, "content");
    }

    public async Task<IReadOnlyList<EisMivTextRow>> GetMivSpecialtyFocusAsync(int mivId, CancellationToken ct)
    {
        return TextRows(await ReadProcedureAsync(_mpVoteContext, "dbo.usp_get_specialtyFocus", mivId, ct), null, "content");
    }

    /// <summary>
    /// Runs a MyInfoVault procedure and reads every column as text by name. These procedures
    /// query MyInfoVault through OPENQUERY, so SQL Server can't describe their result types and
    /// EF's typed SqlQuery can't map them; reading by name also tolerates column-name casing.
    /// </summary>
    private static async Task<List<Dictionary<string, string?>>> ReadProcedureAsync(
        DbContext context, string procedure, int mivId, CancellationToken ct)
    {
        DbConnection connection = context.Database.GetDbConnection();
        bool opened = connection.State != ConnectionState.Open;
        if (opened)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@MIVID", SqlDbType.Int) { Value = mivId });
            await using DbDataReader reader = await command.ExecuteReaderAsync(ct);
            var rows = new List<Dictionary<string, string?>>();
            while (await reader.ReadAsync(ct))
            {
                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = await reader.IsDBNullAsync(i, ct)
                        ? null
                        : Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture);
                }

                rows.Add(row);
            }

            return rows;
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static List<EisMivTextRow> TextRows(List<Dictionary<string, string?>> rows, string? yearColumn, string textColumn)
    {
        return [.. rows.Select(row => new EisMivTextRow(yearColumn is null ? null : Field(row, yearColumn), Field(row, textColumn)))];
    }

    private static string? Field(Dictionary<string, string?> row, string column)
    {
        return row.TryGetValue(column, out string? value) ? value : null;
    }

    private async Task<string?> GetDepartmentAsync(string employeeId, string field, CancellationToken ct)
    {
        SqlParameter id = Id(employeeId);
        SqlParameter deptField = Text("@deptField", field, DepartmentFieldLength);
        List<EisNamedValueRow> rows = await Query<EisNamedValueRow>(
            $"EXEC dbo.usp_eis_getPersonDepartment @employeeID = {id}, @deptField = {deptField}", ct);
        return rows.FirstOrDefault()?.Name;
    }

    private async Task<T?> First<T>(FormattableString sql, CancellationToken ct) where T : class
    {
        return (await Query<T>(sql, ct)).FirstOrDefault();
    }

    private Task<List<T>> Query<T>(FormattableString sql, CancellationToken ct)
    {
        return _context.Database.SqlQuery<T>(sql).ToListAsync(ct);
    }

    private static SqlParameter Id(string employeeId)
    {
        return Text("@employeeID", employeeId, EmployeeIdLength);
    }

    private static SqlParameter Text(string name, string value, int size)
    {
        return new SqlParameter(name, SqlDbType.VarChar, size) { Value = value };
    }
}
