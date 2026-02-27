using System.Data;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

public partial class ZeroEffortService : BaseReportService, IZeroEffortService
{
    private readonly ITermService _termService;
    private readonly ILogger<ZeroEffortService> _logger;

    public ZeroEffortService(
        EffortDbContext context,
        ITermService termService,
        ILogger<ZeroEffortService> logger)
        : base(context)
    {
        _termService = termService;
        _logger = logger;
    }

    public async Task<ZeroEffortReport> GetZeroEffortReportAsync(
        int termCode,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default)
    {
        var term = await _termService.GetTermAsync(termCode, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        var report = new ZeroEffortReport
        {
            TermCode = termCode,
            TermName = term?.TermName ?? _termService.GetTermName(termCode),
            FilterDepartment = filterDept
        };

        report.Instructors = await ExecuteForDepartmentsAsync(termCode, departments, ct);

        _logger.LogDebug("Zero effort report for term {TermCode}: {Count} instructors with zero effort", termCode, report.Instructors.Count);
        return report;
    }

    public async Task<ZeroEffortReport> GetZeroEffortReportByYearAsync(
        string academicYear,
        IReadOnlyList<string>? departments = null,
        CancellationToken ct = default)
    {
        var startYear = ParseAcademicYearStart(academicYear);
        var termCodes = await GetTermCodesForAcademicYearAsync(startYear, ct);
        var filterDept = departments is { Count: 1 } ? departments[0] : null;

        if (termCodes.Count == 0)
        {
            return new ZeroEffortReport
            {
                TermName = academicYear,
                AcademicYear = academicYear,
                FilterDepartment = filterDept
            };
        }

        // Collect instructors across all terms and deduplicate by MothraId
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allInstructors = new List<ZeroEffortInstructorRow>();

        foreach (var tc in termCodes)
        {
            var rows = await ExecuteForDepartmentsAsync(tc, departments, ct);
            allInstructors.AddRange(rows.Where(row => seen.Add(row.MothraId)));
        }

        var report = new ZeroEffortReport
        {
            TermCode = termCodes[0],
            TermName = academicYear,
            AcademicYear = academicYear,
            FilterDepartment = filterDept,
            Instructors = allInstructors
        };

        _logger.LogDebug("Zero effort report for year {AcademicYear}: {Count} instructors with zero effort", LogSanitizer.SanitizeString(academicYear), allInstructors.Count);
        return report;
    }

    /// <summary>
    /// Execute the zero effort SP for each department in the list, once with null for all departments,
    /// or return empty when the list is explicitly empty (unauthorized request).
    /// </summary>
    private async Task<List<ZeroEffortInstructorRow>> ExecuteForDepartmentsAsync(
        int termCode, IReadOnlyList<string>? departments, CancellationToken ct)
    {
        if (departments is { Count: 0 })
        {
            return [];
        }

        if (departments == null)
        {
            return await ExecuteZeroEffortSpAsync(termCode, null, ct);
        }

        if (departments.Count == 1)
        {
            return await ExecuteZeroEffortSpAsync(termCode, departments[0], ct);
        }

        var allRows = new List<ZeroEffortInstructorRow>();
        foreach (var dept in departments)
        {
            var rows = await ExecuteZeroEffortSpAsync(termCode, dept, ct);
            allRows.AddRange(rows);
        }
        return allRows;
    }

    private async Task<List<ZeroEffortInstructorRow>> ExecuteZeroEffortSpAsync(
        int termCode, string? department, CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_zero_effort_check]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode);
        command.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);

        // SP returns: MothraId, FirstName, LastName, MiddleInitial, EffortDept, EffortVerified
        // The SP uses SELECT DISTINCT, so each row is a unique instructor with zero-effort courses.
        var instructors = new List<ZeroEffortInstructorRow>();

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var mothraId = reader.GetString(0);
            var firstName = await reader.IsDBNullAsync(1, ct) ? "" : reader.GetString(1).Trim();
            var lastName = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2).Trim();
            var effortDept = await reader.IsDBNullAsync(4, ct) ? "" : reader.GetString(4).Trim();
            var verified = !await reader.IsDBNullAsync(5, ct) && reader.GetBoolean(5);

            instructors.Add(new ZeroEffortInstructorRow
            {
                MothraId = mothraId.Trim(),
                Instructor = $"{lastName}, {firstName}",
                Department = effortDept,
                JobGroupId = "",
                Verified = verified
            });
        }

        return instructors;
    }
}
