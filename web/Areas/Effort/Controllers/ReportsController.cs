using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for effort reports (teaching activity, dept summary, school summary, merit, zero effort).
/// </summary>
[Route("/api/effort/reports")]
[Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports},{EffortPermissions.SchoolSummary}")]
public partial class ReportsController : BaseEffortController
{
    private readonly ITeachingActivityService _teachingActivityService;
    private readonly IDeptSummaryService _deptSummaryService;
    private readonly ISchoolSummaryService _schoolSummaryService;
    private readonly IMeritReportService _meritReportService;
    private readonly IMeritSummaryService _meritSummaryService;
    private readonly IClinicalEffortService _clinicalEffortService;
    private readonly IClinicalScheduleService _clinicalScheduleService;
    private readonly IZeroEffortService _zeroEffortService;
    private readonly IEffortPermissionService _permissionService;

    [GeneratedRegex(@"^\d{4}-\d{4}$")]
    private static partial Regex AcademicYearFormatRegex();

    public ReportsController(
        ITeachingActivityService teachingActivityService,
        IDeptSummaryService deptSummaryService,
        ISchoolSummaryService schoolSummaryService,
        IMeritReportService meritReportService,
        IMeritSummaryService meritSummaryService,
        IClinicalEffortService clinicalEffortService,
        IClinicalScheduleService clinicalScheduleService,
        IZeroEffortService zeroEffortService,
        IEffortPermissionService permissionService,
        ILogger<ReportsController> logger) : base(logger)
    {
        _teachingActivityService = teachingActivityService;
        _deptSummaryService = deptSummaryService;
        _schoolSummaryService = schoolSummaryService;
        _meritReportService = meritReportService;
        _meritSummaryService = meritSummaryService;
        _clinicalEffortService = clinicalEffortService;
        _clinicalScheduleService = clinicalScheduleService;
        _zeroEffortService = zeroEffortService;
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
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
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
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
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
        var validationResult = ValidateTermAndYear(termCode, academicYear)
            ?? ValidateReportParams(department, personId, role, title);
        if (validationResult != null) return validationResult;

        // Apply department permission filtering
        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(department, authorizedDepartments);

        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Teaching activity report requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            var report = await _teachingActivityService.GetTeachingActivityReportByYearAsync(
                academicYear, effectiveDepartments, personId, role, title, ct);
            FilterReportByAuthorizedDepartments(report, authorizedDepartments);
            return Ok(report);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Teaching activity report requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                termCode,
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            var report = await _teachingActivityService.GetTeachingActivityReportAsync(
                termCode, effectiveDepartments, personId, role, title, ct);
            FilterReportByAuthorizedDepartments(report, authorizedDepartments);
            return Ok(report);
        }
    }

    /// <summary>
    /// Export teaching activity report as PDF.
    /// </summary>
    [HttpPost("teaching/grouped/pdf")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult> ExportTeachingActivityGroupedPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear)
            ?? ValidateReportParams(request.Department, request.PersonId, request.Role, request.Title);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(request.Department, authorizedDepartments);

        TeachingActivityReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            _logger.LogInformation("Teaching activity PDF export requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                LogSanitizer.SanitizeString(request.AcademicYear),
                LogSanitizer.SanitizeString(request.Department),
                request.PersonId,
                LogSanitizer.SanitizeString(request.Role),
                LogSanitizer.SanitizeString(request.Title));

            report = await _teachingActivityService.GetTeachingActivityReportByYearAsync(
                request.AcademicYear, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            _logger.LogInformation("Teaching activity PDF export requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                request.TermCode,
                LogSanitizer.SanitizeString(request.Department),
                request.PersonId,
                LogSanitizer.SanitizeString(request.Role),
                LogSanitizer.SanitizeString(request.Title));

            report = await _teachingActivityService.GetTeachingActivityReportAsync(
                request.TermCode, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
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
    /// Export individual teaching activity report as PDF (one section per instructor).
    /// </summary>
    [HttpPost("teaching/individual/pdf")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult> ExportTeachingActivityIndividualPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear)
            ?? ValidateReportParams(request.Department, request.PersonId, request.Role, request.Title);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(request.Department, authorizedDepartments);

        TeachingActivityReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            _logger.LogInformation("Teaching activity individual PDF export requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                LogSanitizer.SanitizeString(request.AcademicYear),
                LogSanitizer.SanitizeString(request.Department),
                request.PersonId,
                LogSanitizer.SanitizeString(request.Role),
                LogSanitizer.SanitizeString(request.Title));

            report = await _teachingActivityService.GetTeachingActivityReportByYearAsync(
                request.AcademicYear, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            _logger.LogInformation("Teaching activity individual PDF export requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                request.TermCode,
                LogSanitizer.SanitizeString(request.Department),
                request.PersonId,
                LogSanitizer.SanitizeString(request.Role),
                LogSanitizer.SanitizeString(request.Title));

            report = await _teachingActivityService.GetTeachingActivityReportAsync(
                request.TermCode, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.Departments.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _teachingActivityService.GenerateIndividualReportPdfAsync(report);
        var filename = $"TeachingActivityIndividual_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    /// <summary>
    /// Resolve the effective department list for SP calls.
    /// Combines the user's requested department filter with their authorization.
    /// Returns null for admins requesting all departments (SP returns all data).
    /// </summary>
    private static IReadOnlyList<string>? ResolveEffectiveDepartments(
        string? requestedDepartment, List<string>? authorizedDepartments)
    {
        // Admin user — pass through user's filter
        if (authorizedDepartments == null)
        {
            return requestedDepartment != null ? [requestedDepartment] : null;
        }

        // If a specific department was requested, verify it's in the authorized list.
        // Return empty if unauthorized — callers will produce an empty report rather
        // than silently substituting a different department.
        if (!string.IsNullOrEmpty(requestedDepartment))
        {
            return authorizedDepartments.Contains(requestedDepartment, StringComparer.OrdinalIgnoreCase)
                ? [requestedDepartment]
                : [];
        }

        // No department requested — pass all authorized departments so the SP
        // only returns data the user is allowed to see
        return authorizedDepartments;
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

    private static void FilterReportByAuthorizedDepartments(DeptSummaryReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        report.Departments = report.Departments
            .Where(d => authorizedDepartments.Contains(d.Department, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static void FilterReportByAuthorizedDepartments(SchoolSummaryReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        report.Departments = report.Departments
            .Where(d => authorizedDepartments.Contains(d.Department, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Recompute grand totals from the authorized subset to prevent leaking school-wide data
        var effortTypes = report.EffortTypes ?? [];
        var facultyCount = report.Departments.Sum(d => d.FacultyCount);
        var facultyWithCliCount = report.Departments.Sum(d => d.FacultyWithCliCount);
        var totals = new Dictionary<string, decimal>();
        var averages = new Dictionary<string, decimal>();

        foreach (var effortType in effortTypes)
        {
            var total = report.Departments.Sum(d => d.EffortTotals.GetValueOrDefault(effortType));
            if (total != 0)
            {
                totals[effortType] = total;
            }

            var divisor = string.Equals(effortType, "CLI", StringComparison.OrdinalIgnoreCase)
                ? facultyWithCliCount
                : facultyCount;
            if (divisor > 0 && total != 0)
            {
                averages[effortType] = Math.Round(total / divisor, 2);
            }
        }

        report.GrandTotals = new SchoolSummaryTotalsRow
        {
            FacultyCount = facultyCount,
            FacultyWithCliCount = facultyWithCliCount,
            EffortTotals = totals,
            Averages = averages
        };
    }

    private static void FilterReportByAuthorizedDepartments(MeritDetailReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        report.Departments = report.Departments
            .Where(d => authorizedDepartments.Contains(d.Department, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static void FilterReportByAuthorizedDepartments(MeritAverageReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        foreach (var jobGroup in report.JobGroups)
        {
            jobGroup.Departments = jobGroup.Departments
                .Where(d => authorizedDepartments.Contains(d.Department, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        report.JobGroups = report.JobGroups
            .Where(jg => jg.Departments.Count > 0)
            .ToList();
    }

    private static void FilterReportByAuthorizedDepartments(ZeroEffortReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        report.Instructors = report.Instructors
            .Where(i => authorizedDepartments.Contains(i.Department, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    private static void FilterReportByAuthorizedDepartments(MeritSummaryReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        foreach (var jobGroup in report.JobGroups)
        {
            jobGroup.Departments = jobGroup.Departments
                .Where(d => authorizedDepartments.Contains(d.Department, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        report.JobGroups = report.JobGroups
            .Where(jg => jg.Departments.Count > 0)
            .ToList();
    }

    private static void FilterReportByAuthorizedDepartments(ClinicalEffortReport report, List<string>? authorizedDepartments)
    {
        if (authorizedDepartments == null)
        {
            return;
        }

        foreach (var jobGroup in report.JobGroups)
        {
            jobGroup.Instructors = jobGroup.Instructors
                .Where(i => authorizedDepartments.Contains(i.Department, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        report.JobGroups = report.JobGroups
            .Where(jg => jg.Instructors.Count > 0)
            .ToList();
    }

    // ============================================
    // Department Summary Endpoints
    // ============================================

    /// <summary>
    /// Get department summary report. Shows one row per instructor with effort type totals,
    /// plus department totals and averages.
    /// </summary>
    [HttpGet("teaching/dept-summary")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult<DeptSummaryReport>> GetDeptSummary(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        [FromQuery] int? personId = null,
        [FromQuery] string? role = null,
        [FromQuery] string? title = null,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(termCode, academicYear)
            ?? ValidateReportParams(department, personId, role, title);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(department, authorizedDepartments);

        DeptSummaryReport report;
        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Dept summary report requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            report = await _deptSummaryService.GetDeptSummaryReportByYearAsync(
                academicYear, effectiveDepartments, personId, role, title, ct);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Dept summary report requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                termCode,
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            report = await _deptSummaryService.GetDeptSummaryReportAsync(
                termCode, effectiveDepartments, personId, role, title, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);
        return Ok(report);
    }

    /// <summary>
    /// Export department summary report as PDF.
    /// </summary>
    [HttpPost("teaching/dept-summary/pdf")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult> ExportDeptSummaryPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear)
            ?? ValidateReportParams(request.Department, request.PersonId, request.Role, request.Title);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(request.Department, authorizedDepartments);

        DeptSummaryReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            report = await _deptSummaryService.GetDeptSummaryReportByYearAsync(
                request.AcademicYear, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            report = await _deptSummaryService.GetDeptSummaryReportAsync(
                request.TermCode, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.Departments.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _deptSummaryService.GenerateReportPdfAsync(report);
        var filename = $"DeptSummary_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    // ============================================
    // School Summary Endpoints
    // ============================================

    /// <summary>
    /// Get school-wide summary report. Requires SVMSecure.Effort.SchoolSummary permission.
    /// </summary>
    [HttpGet("teaching/school-summary")]
    [Permission(Allow = $"{EffortPermissions.SchoolSummary},{EffortPermissions.ViewAllDepartments}")]
    public async Task<ActionResult<SchoolSummaryReport>> GetSchoolSummary(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        [FromQuery] int? personId = null,
        [FromQuery] string? role = null,
        [FromQuery] string? title = null,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(termCode, academicYear)
            ?? ValidateReportParams(department, personId, role, title);
        if (validationResult != null) return validationResult;

        // SchoolSummary permission grants full-scope access to this report,
        // so bypass department filtering when the user has it.
        var hasSchoolSummaryAccess = await _permissionService.HasPermissionAsync(EffortPermissions.SchoolSummary, ct);
        var authorizedDepartments = hasSchoolSummaryAccess ? null : await GetDepartmentFilterAsync(ct);
        var effectiveDepartments = ResolveEffectiveDepartments(department, authorizedDepartments);

        SchoolSummaryReport report;
        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("School summary report requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            report = await _schoolSummaryService.GetSchoolSummaryReportByYearAsync(
                academicYear, effectiveDepartments, personId, role, title, ct);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("School summary report requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}, title={Title}",
                termCode,
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role),
                LogSanitizer.SanitizeString(title));

            report = await _schoolSummaryService.GetSchoolSummaryReportAsync(
                termCode, effectiveDepartments, personId, role, title, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);
        return Ok(report);
    }

    /// <summary>
    /// Export school summary report as PDF. Requires SVMSecure.Effort.SchoolSummary permission.
    /// </summary>
    [HttpPost("teaching/school-summary/pdf")]
    [Permission(Allow = $"{EffortPermissions.SchoolSummary},{EffortPermissions.ViewAllDepartments}")]
    public async Task<ActionResult> ExportSchoolSummaryPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear)
            ?? ValidateReportParams(request.Department, request.PersonId, request.Role, request.Title);
        if (validationResult != null) return validationResult;

        var hasSchoolSummaryAccess = await _permissionService.HasPermissionAsync(EffortPermissions.SchoolSummary, ct);
        var authorizedDepartments = hasSchoolSummaryAccess ? null : await GetDepartmentFilterAsync(ct);
        var effectiveDepartments = ResolveEffectiveDepartments(request.Department, authorizedDepartments);

        SchoolSummaryReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            report = await _schoolSummaryService.GetSchoolSummaryReportByYearAsync(
                request.AcademicYear, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            report = await _schoolSummaryService.GetSchoolSummaryReportAsync(
                request.TermCode, effectiveDepartments, request.PersonId, request.Role, request.Title, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.Departments.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _schoolSummaryService.GenerateReportPdfAsync(report);
        var filename = $"SchoolSummary_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    // ============================================
    // Merit & Promotion Detail Endpoints
    // ============================================

    /// <summary>
    /// Get merit detail report. Shows course-level effort data grouped by department and instructor.
    /// </summary>
    [HttpGet("merit/detail")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult<MeritDetailReport>> GetMeritDetail(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        [FromQuery] int? personId = null,
        [FromQuery] string? role = null,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(termCode, academicYear)
            ?? ValidateReportParams(department, personId, role);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(department, authorizedDepartments);

        MeritDetailReport report;
        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Merit detail report requested: year={AcademicYear}, dept={Department}, person={PersonId}, role={Role}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role));

            report = await _meritReportService.GetMeritDetailReportByYearAsync(
                academicYear, effectiveDepartments, personId, role, ct);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Merit detail report requested: term={TermCode}, dept={Department}, person={PersonId}, role={Role}",
                termCode,
                LogSanitizer.SanitizeString(department),
                personId,
                LogSanitizer.SanitizeString(role));

            report = await _meritReportService.GetMeritDetailReportAsync(
                termCode, effectiveDepartments, personId, role, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);
        return Ok(report);
    }

    /// <summary>
    /// Export merit detail report as PDF.
    /// </summary>
    [HttpPost("merit/detail/pdf")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult> ExportMeritDetailPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear)
            ?? ValidateReportParams(request.Department, request.PersonId, request.Role);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(request.Department, authorizedDepartments);

        MeritDetailReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            report = await _meritReportService.GetMeritDetailReportByYearAsync(
                request.AcademicYear, effectiveDepartments, request.PersonId, request.Role, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            report = await _meritReportService.GetMeritDetailReportAsync(
                request.TermCode, effectiveDepartments, request.PersonId, request.Role, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.Departments.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _meritReportService.GenerateMeritDetailPdfAsync(report);
        var filename = $"MeritDetail_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    // ============================================
    // Merit & Promotion Average Endpoints
    // ============================================

    /// <summary>
    /// Get merit average report. Shows instructor effort totals grouped by job group and department.
    /// </summary>
    [HttpGet("merit/average")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult<MeritAverageReport>> GetMeritAverage(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        [FromQuery] int? personId = null,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(termCode, academicYear)
            ?? ValidateReportParams(department, personId);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(department, authorizedDepartments);

        MeritAverageReport report;
        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Merit average report requested: year={AcademicYear}, dept={Department}, person={PersonId}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(department),
                personId);

            report = await _meritReportService.GetMeritAverageReportByYearAsync(
                academicYear, effectiveDepartments, personId, ct);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Merit average report requested: term={TermCode}, dept={Department}, person={PersonId}",
                termCode,
                LogSanitizer.SanitizeString(department),
                personId);

            report = await _meritReportService.GetMeritAverageReportAsync(
                termCode, effectiveDepartments, personId, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);
        return Ok(report);
    }

    /// <summary>
    /// Export merit average report as PDF.
    /// </summary>
    [HttpPost("merit/average/pdf")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult> ExportMeritAveragePdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear)
            ?? ValidateReportParams(request.Department, request.PersonId);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(request.Department, authorizedDepartments);

        MeritAverageReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            report = await _meritReportService.GetMeritAverageReportByYearAsync(
                request.AcademicYear, effectiveDepartments, request.PersonId, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            report = await _meritReportService.GetMeritAverageReportAsync(
                request.TermCode, effectiveDepartments, request.PersonId, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.JobGroups.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _meritReportService.GenerateMeritAveragePdfAsync(report);
        var filename = $"MeritAverage_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    // ============================================
    // Merit & Promotion Summary Endpoints
    // ============================================

    /// <summary>
    /// Get merit summary report. Shows department totals and averages grouped by job group.
    /// </summary>
    [HttpGet("merit/summary")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult<MeritSummaryReport>> GetMeritSummary(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(termCode, academicYear)
            ?? ValidateReportParams(department);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(department, authorizedDepartments);

        MeritSummaryReport report;
        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Merit summary report requested: year={AcademicYear}, dept={Department}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(department));

            report = await _meritSummaryService.GetMeritSummaryReportByYearAsync(
                academicYear, effectiveDepartments, ct);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Merit summary report requested: term={TermCode}, dept={Department}",
                termCode,
                LogSanitizer.SanitizeString(department));

            report = await _meritSummaryService.GetMeritSummaryReportAsync(
                termCode, effectiveDepartments, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);
        return Ok(report);
    }

    /// <summary>
    /// Export merit summary report as PDF.
    /// </summary>
    [HttpPost("merit/summary/pdf")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult> ExportMeritSummaryPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear)
            ?? ValidateReportParams(request.Department);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(request.Department, authorizedDepartments);

        MeritSummaryReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            report = await _meritSummaryService.GetMeritSummaryReportByYearAsync(
                request.AcademicYear, effectiveDepartments, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            report = await _meritSummaryService.GetMeritSummaryReportAsync(
                request.TermCode, effectiveDepartments, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.JobGroups.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _meritSummaryService.GenerateReportPdfAsync(report);
        var filename = $"MeritSummary_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    // ============================================
    // Clinical Effort Endpoints
    // ============================================

    /// <summary>
    /// Get clinical effort report. Shows instructors with clinical percent assignments
    /// and their effort data for a given academic year and clinical type (VMTH/CAHFS).
    /// </summary>
    [HttpGet("merit/clinical")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult<ClinicalEffortReport>> GetClinicalEffort(
        [FromQuery] string? academicYear = null,
        [FromQuery] int clinicalType = 0,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(academicYear))
        {
            return BadRequest("academicYear is required.");
        }

        if (!AcademicYearFormatRegex().IsMatch(academicYear))
        {
            return BadRequest("academicYear must be in format YYYY-YYYY (e.g., 2024-2025).");
        }

        if (clinicalType is not (1 or 25))
        {
            return BadRequest("clinicalType must be 1 (VMTH) or 25 (CAHFS).");
        }

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        SetExceptionContext("academicYear", academicYear);
        _logger.LogInformation("Clinical effort report requested: year={AcademicYear}, type={ClinicalType}",
            LogSanitizer.SanitizeString(academicYear),
            clinicalType);

        var report = await _clinicalEffortService.GetClinicalEffortReportAsync(
            academicYear, clinicalType, ct);
        FilterReportByAuthorizedDepartments(report, authorizedDepartments);
        return Ok(report);
    }

    /// <summary>
    /// Export clinical effort report as PDF.
    /// </summary>
    [HttpPost("merit/clinical/pdf")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult> ExportClinicalEffortPdf(
        [FromBody] ClinicalEffortPdfRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.AcademicYear))
        {
            return BadRequest("academicYear is required.");
        }

        if (!AcademicYearFormatRegex().IsMatch(request.AcademicYear))
        {
            return BadRequest("academicYear must be in format YYYY-YYYY (e.g., 2024-2025).");
        }

        if (request.ClinicalType is not (1 or 25))
        {
            return BadRequest("clinicalType must be 1 (VMTH) or 25 (CAHFS).");
        }

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        SetExceptionContext("academicYear", request.AcademicYear);

        var report = await _clinicalEffortService.GetClinicalEffortReportAsync(
            request.AcademicYear, request.ClinicalType, ct);
        FilterReportByAuthorizedDepartments(report, authorizedDepartments);

        if (report.JobGroups.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _clinicalEffortService.GenerateReportPdfAsync(report);
        var filename = $"ClinicalEffort_{report.ClinicalTypeName}_{report.AcademicYear}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    // ============================================
    // Scheduled CLI Weeks Endpoints
    // ============================================

    /// <summary>
    /// Get scheduled clinical weeks report from Clinical Scheduler.
    /// Shows weeks scheduled per instructor from live scheduler data.
    /// </summary>
    [HttpGet("clinical-schedule")]
    [Permission(Allow = EffortPermissions.ViewAllDepartments)]
    public async Task<ActionResult<ScheduledCliWeeksReport>> GetScheduledCliWeeks(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(termCode, academicYear);
        if (validationResult != null) return validationResult;

        // Clinical schedule data lacks department info — restrict to full-access users
        if (!await _permissionService.HasFullAccessAsync(ct))
        {
            return Forbid();
        }

        ScheduledCliWeeksReport report;
        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Scheduled CLI weeks report requested: year={AcademicYear}",
                LogSanitizer.SanitizeString(academicYear));

            report = await _clinicalScheduleService.GetScheduledCliWeeksReportByYearAsync(
                academicYear, ct);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Scheduled CLI weeks report requested: term={TermCode}", termCode);

            report = await _clinicalScheduleService.GetScheduledCliWeeksReportAsync(
                termCode, ct);
        }

        return Ok(report);
    }

    /// <summary>
    /// Export scheduled clinical weeks report as PDF.
    /// </summary>
    [HttpPost("clinical-schedule/pdf")]
    [Permission(Allow = EffortPermissions.ViewAllDepartments)]
    public async Task<ActionResult> ExportScheduledCliWeeksPdf(
        [FromBody] ReportPdfRequest request,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(request.TermCode, request.AcademicYear);
        if (validationResult != null) return validationResult;

        if (!await _permissionService.HasFullAccessAsync(ct))
        {
            return Forbid();
        }

        ScheduledCliWeeksReport report;
        if (!string.IsNullOrEmpty(request.AcademicYear))
        {
            SetExceptionContext("academicYear", request.AcademicYear);
            report = await _clinicalScheduleService.GetScheduledCliWeeksReportByYearAsync(
                request.AcademicYear, ct);
        }
        else
        {
            SetExceptionContext("termCode", request.TermCode);
            report = await _clinicalScheduleService.GetScheduledCliWeeksReportAsync(
                request.TermCode, ct);
        }

        if (report.Instructors.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = await _clinicalScheduleService.GenerateReportPdfAsync(report);
        var filename = $"ScheduledClinicalWeeks_{report.TermName}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    // ============================================
    // Zero Effort Endpoint
    // ============================================

    /// <summary>
    /// Get zero effort report. Identifies instructors with courses assigned but zero effort recorded.
    /// Accepts either termCode (single term) or academicYear (e.g., "2024-2025") for multi-term.
    /// </summary>
    [HttpGet("zero-effort")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept},{EffortPermissions.Reports}")]
    public async Task<ActionResult<ZeroEffortReport>> GetZeroEffort(
        [FromQuery] int termCode = 0,
        [FromQuery] string? academicYear = null,
        [FromQuery] string? department = null,
        CancellationToken ct = default)
    {
        var validationResult = ValidateTermAndYear(termCode, academicYear)
            ?? ValidateReportParams(department);
        if (validationResult != null) return validationResult;

        var authorizedDepartments = await GetDepartmentFilterAsync(ct);
        if (authorizedDepartments is { Count: 0 })
        {
            return Forbid();
        }

        var effectiveDepartments = ResolveEffectiveDepartments(department, authorizedDepartments);

        ZeroEffortReport report;
        if (!string.IsNullOrEmpty(academicYear))
        {
            SetExceptionContext("academicYear", academicYear);
            _logger.LogInformation("Zero effort report requested: year={AcademicYear}, dept={Department}",
                LogSanitizer.SanitizeString(academicYear),
                LogSanitizer.SanitizeString(department));

            report = await _zeroEffortService.GetZeroEffortReportByYearAsync(academicYear, effectiveDepartments, ct);
        }
        else
        {
            SetExceptionContext("termCode", termCode);
            _logger.LogInformation("Zero effort report requested: term={TermCode}, dept={Department}",
                termCode,
                LogSanitizer.SanitizeString(department));

            report = await _zeroEffortService.GetZeroEffortReportAsync(termCode, effectiveDepartments, ct);
        }

        FilterReportByAuthorizedDepartments(report, authorizedDepartments);
        return Ok(report);
    }

    // ============================================
    // Shared Validation
    // ============================================

    /// <summary>
    /// Validate optional report filter parameters. Returns an error ActionResult if invalid, or null if valid.
    /// </summary>
    private ActionResult? ValidateReportParams(
        string? department = null, int? personId = null, string? role = null, string? title = null)
    {
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

        return null;
    }

    /// <summary>
    /// Validate termCode and academicYear parameters. Returns an error ActionResult if invalid, or null if valid.
    /// </summary>
    private ActionResult? ValidateTermAndYear(int termCode, string? academicYear)
    {
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

        return null;
    }
}
