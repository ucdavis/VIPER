using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Shared infrastructure for effort report services: academic year resolution,
/// general report SP execution, and effort type column ordering for PDFs.
/// </summary>
public abstract partial class BaseReportService
{
    protected readonly EffortDbContext _context;

    protected BaseReportService(EffortDbContext context)
    {
        _context = context;
    }

    // ── Academic Year ────────────────────────────────────────────────

    [GeneratedRegex(@"^(\d{4})-(\d{4})$")]
    protected static partial Regex AcademicYearRegex();

    /// <summary>
    /// Parse the start year from an "YYYY-YYYY" academic year string.
    /// Throws ArgumentException if the format is invalid.
    /// </summary>
    protected static int ParseAcademicYearStart(string academicYear)
    {
        var match = AcademicYearRegex().Match(academicYear);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid academic year format: {academicYear}. Expected format: YYYY-YYYY");
        }
        return int.Parse(match.Groups[1].Value);
    }

    /// <summary>
    /// Get all term codes that belong to the given academic year, ordered descending.
    /// </summary>
    protected async Task<List<int>> GetTermCodesForAcademicYearAsync(int startYear, CancellationToken ct)
    {
        var allTerms = await _context.Terms
            .Select(t => t.TermCode)
            .ToListAsync(ct);

        return AcademicYearHelper.GetTermCodesForAcademicYear(allTerms, startYear);
    }

    // ── General Report SP ───────────────────────────────────────────

    /// <summary>
    /// Execute effort.sp_effort_general_report and map raw rows.
    /// Used by TeachingActivityService, DeptSummaryService, and SchoolSummaryService.
    /// </summary>
    protected async Task<List<TeachingActivityRow>> ExecuteGeneralReportSpAsync(
        int termCode,
        string? department,
        int? personId,
        string? role,
        string? jobGroupId,
        CancellationToken ct)
    {
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string not configured");

        var results = new List<TeachingActivityRow>();

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        await using var command = new Microsoft.Data.SqlClient.SqlCommand("[effort].[sp_effort_general_report]", connection);
        command.CommandType = CommandType.StoredProcedure;
        command.Parameters.AddWithValue("@TermCode", termCode);
        command.Parameters.AddWithValue("@Department", (object?)department ?? DBNull.Value);
        command.Parameters.AddWithValue("@PersonId", personId.HasValue ? personId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@Role", (object?)role ?? DBNull.Value);
        command.Parameters.AddWithValue("@JobGroupId", (object?)jobGroupId ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new TeachingActivityRow
            {
                TermCode = termCode,
                MothraId = reader.GetString(0),
                Instructor = reader.GetString(1),
                JobGroupId = await reader.IsDBNullAsync(2, ct) ? "" : reader.GetString(2),
                Department = await reader.IsDBNullAsync(3, ct) ? "" : reader.GetString(3),
                CourseId = reader.GetInt32(4),
                Course = reader.GetString(5),
                Crn = await reader.IsDBNullAsync(6, ct) ? "" : reader.GetString(6),
                Units = await reader.IsDBNullAsync(7, ct) ? 0m : reader.GetDecimal(7),
                Enrollment = await reader.IsDBNullAsync(8, ct) ? 0 : reader.GetInt32(8),
                RoleId = await reader.IsDBNullAsync(9, ct) ? "" : reader.GetString(9),
                EffortTypeId = await reader.IsDBNullAsync(10, ct) ? "" : reader.GetString(10),
                Hours = await reader.IsDBNullAsync(11, ct) ? 0m : reader.GetInt32(11),
                Weeks = await reader.IsDBNullAsync(12, ct) ? 0m : reader.GetInt32(12)
            });
        }

        return results;
    }

    /// <summary>
    /// Execute the general report SP for each department in the list, once with null for all departments,
    /// or return empty when the list is explicitly empty (unauthorized request).
    /// </summary>
    protected async Task<List<TeachingActivityRow>> ExecuteGeneralReportForDepartmentsAsync(
        int termCode, IReadOnlyList<string>? departments,
        int? personId, string? role, string? jobGroupId, CancellationToken ct)
    {
        if (departments is { Count: 0 })
        {
            return [];
        }

        if (departments == null)
        {
            return await ExecuteGeneralReportSpAsync(termCode, null, personId, role, jobGroupId, ct);
        }

        if (departments.Count == 1)
        {
            return await ExecuteGeneralReportSpAsync(termCode, departments[0], personId, role, jobGroupId, ct);
        }

        var allRows = new List<TeachingActivityRow>();
        foreach (var dept in departments)
        {
            var rows = await ExecuteGeneralReportSpAsync(termCode, dept, personId, role, jobGroupId, ct);
            allRows.AddRange(rows);
        }
        return allRows;
    }

    // ── Clinical Faculty Lookup ─────────────────────────────────────

    /// <summary>
    /// Gets MothraIds of instructors with clinical percent assignments for the academic year.
    /// Used by summary reports to count "Faculty with CLI assigned" matching legacy behavior.
    /// </summary>
    protected async Task<HashSet<string>> GetClinicalFacultyMothraIdsAsync(string academicYear, CancellationToken ct)
    {
        var mothraIds = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Where(p => p.AcademicYear == academicYear
                && p.PercentageValue > 0
                && p.PercentAssignType.Class == "Clinical")
            .Select(p => p.PersonId)
            .Distinct()
            .Join(_context.ViperPersons.AsNoTracking(),
                personId => personId,
                vp => vp.PersonId,
                (personId, vp) => vp.MothraId)
            .ToListAsync(ct);

        return new HashSet<string>(mothraIds, StringComparer.OrdinalIgnoreCase);
    }

    // ── Averaging ────────────────────────────────────────────────────

    /// <summary>
    /// Calculate averages for effort types with CLI-specific divisor logic.
    /// CLI uses facultyWithCliCount; all others use facultyCount.
    /// Rounds to 1 decimal place to match legacy numberFormat('9.9').
    /// </summary>
    protected static Dictionary<string, decimal> CalculateAverages(
        Dictionary<string, decimal> totals,
        IEnumerable<string> effortTypes,
        int facultyCount,
        int facultyWithCliCount)
    {
        var averages = new Dictionary<string, decimal>();
        foreach (var effortType in effortTypes)
        {
            var total = totals.GetValueOrDefault(effortType);
            if (total == 0) continue;

            var divisor = string.Equals(effortType, "CLI", StringComparison.OrdinalIgnoreCase)
                ? facultyWithCliCount
                : facultyCount;

            if (divisor > 0)
            {
                averages[effortType] = Math.Round(total / divisor, 1);
            }
        }
        return averages;
    }

    // ── PDF Filter Line ─────────────────────────────────────────────

    /// <summary>
    /// Render a "Filters: Dept: All  Role: All  ..." line in a PDF header column.
    /// Each caller passes the label/value pairs relevant to its report type.
    /// Null values display as "All".
    /// </summary>
    protected static void AddPdfFilterLine(IContainer container, params (string Label, string? Value)[] filters)
    {
        container.Row(row =>
        {
            row.AutoItem().PaddingRight(8).Text("Filters:").Bold().FontSize(9);
            foreach (var (label, value) in filters)
            {
                row.RelativeItem().Text($"{label}: {value ?? "All"}").FontSize(9);
            }
        });
    }

    // ── Effort Type Column Ordering (PDF) ───────────────────────────

    /// <summary>
    /// Fixed effort type column order matching legacy reports.
    /// </summary>
    protected static readonly string[] AlwaysShowEffortTypes = ["CLI", "VAR", "LEC", "LAB", "DIS", "PBL", "CBL", "TBL", "PRS", "JLC", "EXM"];

    /// <summary>
    /// Columns that get extra right-padding in report tables.
    /// </summary>
    protected static readonly HashSet<string> SpacerColumns = new() { "VAR", "EXM" };

    /// <summary>
    /// Order effort types for PDF display: fixed columns first, then any extras alphabetically.
    /// </summary>
    protected static List<string> GetOrderedEffortTypes(List<string> effortTypes)
    {
        var ordered = new List<string>(AlwaysShowEffortTypes);
        var remaining = effortTypes
            .Where(t => !AlwaysShowEffortTypes.Contains(t))
            .OrderBy(t => t)
            .ToList();
        ordered.AddRange(remaining);
        return ordered;
    }
}
