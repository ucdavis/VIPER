using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for effort reports (teaching activity, school summary).
/// </summary>
[Route("/api/effort/reports")]
[Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
public partial class ReportsController : BaseEffortController
{
    private readonly ITeachingActivityService _teachingActivityService;
    private readonly IEffortPermissionService _permissionService;

    [GeneratedRegex(@"^\d{4}-\d{4}$")]
    private static partial Regex AcademicYearFormatRegex();

    public ReportsController(
        ITeachingActivityService teachingActivityService,
        IEffortPermissionService permissionService,
        ILogger<ReportsController> logger) : base(logger)
    {
        _teachingActivityService = teachingActivityService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Get authorized department filter for current user.
    /// Returns null for admins (no filter), or list of department codes for ViewDept users.
    /// </summary>
    private async Task<List<string>?> GetDepartmentFilterAsync(CancellationToken ct)
    {
        if (await _permissionService.HasFullAccessAsync(ct))
        {
            return null;
        }

        var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
        return authorizedDepts.Count > 0 ? authorizedDepts : [];
    }

    /// <summary>
    /// Get grouped teaching activity report.
    /// Results are grouped by department, then instructor, then course.
    /// Accepts either termCode (single term) or academicYear (e.g., "2024-2025") for multi-term.
    /// </summary>
    [HttpGet("teaching/grouped")]
    public async Task<ActionResult<TeachingActivityReport>> GetTeachingActivityGrouped(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        [FromQuery] int? personId = null,
        [FromQuery] string? role = null,
        [FromQuery] string? title = null,
        CancellationToken ct = default)
    {
        return await GetTeachingActivityReportAsync(termCode, academicYear, department, personId, role, title, ct);
    }

    /// <summary>
    /// Get individual teaching activity report.
    /// Returns the same data structure as grouped; frontend renders differently.
    /// Accepts either termCode (single term) or academicYear (e.g., "2024-2025") for multi-term.
    /// </summary>
    [HttpGet("teaching/individual")]
    public async Task<ActionResult<TeachingActivityReport>> GetTeachingActivityIndividual(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        [FromQuery] int? personId = null,
        [FromQuery] string? role = null,
        [FromQuery] string? title = null,
        CancellationToken ct = default)
    {
        return await GetTeachingActivityReportAsync(termCode, academicYear, department, personId, role, title, ct);
    }

    private async Task<ActionResult<TeachingActivityReport>> GetTeachingActivityReportAsync(
        int termCode, string? academicYear, string? department, int? personId,
        string? role, string? title, CancellationToken ct)
    {
        // Validate: at least one of termCode or academicYear is required
        if (termCode <= 0 && string.IsNullOrEmpty(academicYear))
        {
            return BadRequest("Either termCode or academicYear is required.");
        }

        if (!string.IsNullOrEmpty(academicYear) && !AcademicYearFormatRegex().IsMatch(academicYear))
        {
            return BadRequest("academicYear must be in format YYYY-YYYY (e.g., 2024-2025).");
        }

        if (!string.IsNullOrEmpty(academicYear))
        {
            var parts = academicYear.Split('-');
            if (int.TryParse(parts[0], out var startYear) && int.TryParse(parts[1], out var endYear)
                && endYear != startYear + 1)
            {
                return BadRequest("academicYear must be a consecutive range (e.g., 2024-2025).");
            }
        }

        if (department != null && department.Length > 6)
        {
            return BadRequest("department must be 6 characters or fewer.");
        }

        if (personId.HasValue && personId.Value <= 0)
        {
            return BadRequest("personId must be greater than 0.");
        }

        if (role != null && role.Length > 1)
        {
            return BadRequest("role must be a single character.");
        }

        if (title != null && title.Length > 3)
        {
            return BadRequest("title must be 3 characters or fewer.");
        }

        // Apply department permission filtering
        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartment = ApplyDepartmentFilterAsync(department, authorizedDepartments);

        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Teaching activity report requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(effectiveDepartment),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            var report = await _teachingActivityService.GetTeachingActivityReportByYearAsync(
                academicYear, effectiveDepartment, personId, role, title, ct);
            FilterReportByAuthorizedDepartments(report, authorizedDepartments);
            return Ok(report);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Teaching activity report requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                termCode,
                LogSanitizer.SanitizeString(effectiveDepartment),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            var report = await _teachingActivityService.GetTeachingActivityReportAsync(
                termCode, effectiveDepartment, personId, role, title, ct);
            FilterReportByAuthorizedDepartments(report, authorizedDepartments);
            return Ok(report);
        }
    }

    /// <summary>
    /// Export teaching activity report as PDF.
    /// </summary>
    [HttpPost("teaching/grouped/pdf")]
    public async Task<ActionResult> ExportTeachingActivityGroupedPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        // Validate: at least one of termCode or academicYear is required
        if (request.TermCode <= 0 && string.IsNullOrEmpty(request.AcademicYear))
        {
            return BadRequest("Either termCode or academicYear is required.");
        }

        if (!string.IsNullOrEmpty(request.AcademicYear) && !AcademicYearFormatRegex().IsMatch(request.AcademicYear))
        {
            return BadRequest("academicYear must be in format YYYY-YYYY (e.g., 2024-2025).");
        }

        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            var parts = request.AcademicYear.Split('-');
            if (int.TryParse(parts[0], out var startYear) && int.TryParse(parts[1], out var endYear)
                && endYear != startYear + 1)
            {
                return BadRequest("academicYear must be a consecutive range (e.g., 2024-2025).");
            }
        }

        if (request.Department != null && request.Department.Length > 6)
        {
            return BadRequest("department must be 6 characters or fewer.");
        }

        if (request.PersonId.HasValue && request.PersonId.Value <= 0)
        {
            return BadRequest("personId must be greater than 0.");
        }

        if (request.Role != null && request.Role.Length > 1)
        {
            return BadRequest("role must be a single character.");
        }

        if (request.Title != null && request.Title.Length > 3)
        {
            return BadRequest("title must be 3 characters or fewer.");
        }

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartment = ApplyDepartmentFilterAsync(request.Department, authorizedDepartments);

        TeachingActivityReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            _logger.LogInformation("Teaching activity PDF export requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                LogSanitizer.SanitizeString(request.AcademicYear),
                LogSanitizer.SanitizeString(effectiveDepartment),
                request.PersonId,
                LogSanitizer.SanitizeString(request.Role),
                LogSanitizer.SanitizeString(request.Title));

            report = await _teachingActivityService.GetTeachingActivityReportByYearAsync(
                request.AcademicYear, effectiveDepartment, request.PersonId, request.Role, request.Title, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            _logger.LogInformation("Teaching activity PDF export requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                request.TermCode,
                LogSanitizer.SanitizeString(effectiveDepartment),
                request.PersonId,
                LogSanitizer.SanitizeString(request.Role),
                LogSanitizer.SanitizeString(request.Title));

            report = await _teachingActivityService.GetTeachingActivityReportAsync(
                request.TermCode, effectiveDepartment, request.PersonId, request.Role, request.Title, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.Departments.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _teachingActivityService.GenerateReportPdfAsync(report);
        var filename = $"TeachingActivity_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    /// <summary>
    /// Apply department permission filtering to the requested department filter.
    /// If user has ViewDept access only, restrict to their authorized departments.
    /// </summary>
    private static string? ApplyDepartmentFilterAsync(string? requestedDepartment, List<string>? authorizedDepartments)
    {
        // Admin user — no restriction
        if (authorizedDepartments == null)
        {
            return requestedDepartment;
        }

        // If a specific department was requested, verify it's in the authorized list
        if (!string.IsNullOrEmpty(requestedDepartment))
        {
            if (authorizedDepartments.Contains(requestedDepartment, StringComparer.OrdinalIgnoreCase))
            {
                return requestedDepartment;
            }

            // Requested department not authorized — return first authorized dept
            // so the SP returns scoped data rather than nothing
            return authorizedDepartments[0];
        }

        // No department requested — if user has exactly one dept, use it;
        // otherwise return null and let the SP return all (post-filtered below)
        return authorizedDepartments.Count == 1 ? authorizedDepartments[0] : null;
    }

    /// <summary>
    /// Post-filter report departments to only those the user is authorized to see.
    /// For admin users (authorizedDepartments == null), no filtering is applied.
    /// </summary>
    private static void FilterReportByAuthorizedDepartments(TeachingActivityReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        report.Departments = report.Departments
            .Where(d => authorizedDepartments.Contains(d.Department, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
