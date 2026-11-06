using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models.PersonCollector;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Personnel.Services;

/// <summary>One person as the legacy Person Collector procedures return them, every value as text.</summary>
public sealed record PersonCollectorRow(
    string? Name,
    string? Email,
    string? LoginId,
    string? EmployeeId,
    string? MothraId,
    string? MailId,
    string? Pidm,
    string? BannerId);

/// <summary>The current quarter and semester term codes, which the student procedure needs.</summary>
public sealed record PersonCollectorTerms(string? Quarter, string? Semester);

/// <summary>
/// Calls the legacy Person Collector procedures in AAUD. Only calls and maps; the rules for
/// which procedures to call live in <see cref="PersonCollectorPlan"/>.
/// </summary>
public interface IPersonCollectorDataService
{
    Task<IReadOnlyList<PersonCollectorRow>> GetDepartmentAsync(string departmentList, CancellationToken ct);

    Task<IReadOnlyList<PersonCollectorRow>> GetFacultyAsync(PersonCollectorFacultyArgs args, CancellationToken ct);

    Task<IReadOnlyList<PersonCollectorRow>> GetStaffAsync(PersonCollectorStaffArgs args, CancellationToken ct);

    Task<IReadOnlyList<PersonCollectorRow>> GetStudentsAsync(
        PersonCollectorStudentArgs args, PersonCollectorTerms terms, CancellationToken ct);

    Task<PersonCollectorTerms> GetCurrentTermsAsync(CancellationToken ct);
}

[ExcludeFromCodeCoverage(Justification = "Only issues SQL against AAUD and VIPER; PersonCollectorService is tested against a fake.")]
public class PersonCollectorDataService : IPersonCollectorDataService
{
    // The term-code suffixes (YYYYTT) that are quarters and semesters; see TermCodeService.
    private static readonly int[] QuarterSuffixes = [1, 3, 8, 10];
    private static readonly int[] SemesterSuffixes = [2, 4, 9];
    private const int TermSuffixDivisor = 100;
    private const int ListParameterSize = 4000;
    private const int TermParameterSize = 6;

    private readonly AAUDContext _aaudContext;
    private readonly VIPERContext _viperContext;

    public PersonCollectorDataService(AAUDContext aaudContext, VIPERContext viperContext)
    {
        _aaudContext = aaudContext;
        _viperContext = viperContext;
    }

    public Task<IReadOnlyList<PersonCollectorRow>> GetDepartmentAsync(string departmentList, CancellationToken ct)
    {
        return ReadAsync("dbo.usp_get_personCollectorDepartment", [List("@departmentList", departmentList)], ct);
    }

    public Task<IReadOnlyList<PersonCollectorRow>> GetFacultyAsync(PersonCollectorFacultyArgs args, CancellationToken ct)
    {
        return ReadAsync(
            "dbo.usp_get_personCollectorSenateFaculty",
            [List("@departmentList", args.DepartmentList), List("@jobGroups", args.JobGroups), Bit("@emeriti", args.Emeriti)],
            ct);
    }

    public Task<IReadOnlyList<PersonCollectorRow>> GetStaffAsync(PersonCollectorStaffArgs args, CancellationToken ct)
    {
        return ReadAsync(
            "dbo.usp_get_personCollectorStaff",
            [
                List("@departmentList", args.DepartmentList),
                Bit("@includeMSP", args.IncludeMsp),
                Bit("@includePSS", args.IncludePss),
                List("@vetJobGroups", args.VetJobGroups),
            ],
            ct);
    }

    public Task<IReadOnlyList<PersonCollectorRow>> GetStudentsAsync(
        PersonCollectorStudentArgs args, PersonCollectorTerms terms, CancellationToken ct)
    {
        return ReadAsync(
            "dbo.usp_get_personCollectorStudents",
            [
                Term("@currentQuarterTerm", terms.Quarter),
                Term("@currentSemesterTerm", terms.Semester),
                List("@classCodes", args.ClassCodes),
                List("@degreeCodes", args.DegreeCodes),
            ],
            ct);
    }

    public async Task<PersonCollectorTerms> GetCurrentTermsAsync(CancellationToken ct)
    {
        // VIPER's terms table flags the current term of each calendar (quarter and semester).
        List<int> current = await _viperContext.Terms
            .AsNoTracking()
            .Where(term => term.CurrentTermMulti)
            .Select(term => term.TermCode)
            .ToListAsync(ct);
        return new PersonCollectorTerms(Latest(current, QuarterSuffixes), Latest(current, SemesterSuffixes));
    }

    private static string? Latest(List<int> termCodes, int[] suffixes)
    {
        return termCodes
            .Where(code => suffixes.Contains(code % TermSuffixDivisor))
            .Select(code => (int?)code)
            .Max()?.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Runs a procedure and reads its columns by name as text. Reading by name keeps this
    /// independent of column order and of the ID columns' types, which differ between procedures.
    /// </summary>
    private async Task<IReadOnlyList<PersonCollectorRow>> ReadAsync(
        string procedure, SqlParameter[] parameters, CancellationToken ct)
    {
        DbConnection connection = _aaudContext.Database.GetDbConnection();
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
            command.Parameters.AddRange(parameters);
            await using DbDataReader reader = await command.ExecuteReaderAsync(ct);
            Dictionary<string, int> columns = Enumerable.Range(0, reader.FieldCount)
                .ToDictionary(reader.GetName, i => i, StringComparer.OrdinalIgnoreCase);
            var rows = new List<PersonCollectorRow>();
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new PersonCollectorRow(
                    await TextAsync(reader, columns, "PERSON_NAME", ct),
                    await TextAsync(reader, columns, "EMAIL", ct),
                    await TextAsync(reader, columns, "LOGIN_ID", ct),
                    await TextAsync(reader, columns, "EMPLOYEE_ID", ct),
                    await TextAsync(reader, columns, "MOTHRA_ID", ct),
                    await TextAsync(reader, columns, "MAIL_ID", ct),
                    await TextAsync(reader, columns, "PIDM", ct),
                    await TextAsync(reader, columns, "BANNER_ID", ct)));
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

    private static async Task<string?> TextAsync(
        DbDataReader reader, Dictionary<string, int> columns, string name, CancellationToken ct)
    {
        if (!columns.TryGetValue(name, out int ordinal) || await reader.IsDBNullAsync(ordinal, ct))
        {
            return null;
        }

        return Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static SqlParameter List(string name, string? value)
    {
        return new SqlParameter(name, SqlDbType.VarChar, ListParameterSize) { Value = (object?)value ?? DBNull.Value };
    }

    private static SqlParameter Term(string name, string? value)
    {
        return new SqlParameter(name, SqlDbType.VarChar, TermParameterSize) { Value = (object?)value ?? DBNull.Value };
    }

    private static SqlParameter Bit(string name, bool value)
    {
        return new SqlParameter(name, SqlDbType.Bit) { Value = value };
    }
}
