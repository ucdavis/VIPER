using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for dashboard statistics and data hygiene operations.
/// Provides bird's eye view for staff/admins to track effort verification progress.
/// ViewDept users see a scoped view filtered to their authorized departments.
/// </summary>
[Route("/api/effort/dashboard")]
[Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewDept}")]
public class DashboardController : BaseEffortController
{
    private readonly IDashboardService _dashboardService;
    private readonly IEffortPermissionService _permissionService;

    public DashboardController(
        IDashboardService dashboardService,
        IEffortPermissionService permissionService,
        ILogger<DashboardController> logger) : base(logger)
    {
        _dashboardService = dashboardService;
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
    /// Get dashboard statistics for a term.
    /// </summary>
    [HttpGet("{termCode:int}/stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var deptFilter = await GetDepartmentFilterAsync(ct);
        var stats = await _dashboardService.GetDashboardStatsAsync(termCode, deptFilter, ct);

        // Set audit access flag for frontend conditional rendering
        stats.HasAuditAccess = await HasAuditAccessAsync(ct);

        return Ok(stats);
    }

    /// <summary>
    /// Check if current user has any audit viewing permission.
    /// </summary>
    private async Task<bool> HasAuditAccessAsync(CancellationToken ct)
    {
        return await _permissionService.HasPermissionAsync(EffortPermissions.ViewAudit, ct) ||
               await _permissionService.HasPermissionAsync(EffortPermissions.ViewDeptAudit, ct) ||
               await _permissionService.HasFullAccessAsync(ct);
    }

    /// <summary>
    /// Get department verification breakdown.
    /// </summary>
    [HttpGet("{termCode:int}/departments")]
    public async Task<ActionResult<IEnumerable<DepartmentVerificationDto>>> GetDepartmentVerification(
        int termCode,
        [FromQuery] int threshold = 80,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        var deptFilter = await GetDepartmentFilterAsync(ct);
        var departments = await _dashboardService.GetDepartmentVerificationAsync(termCode, deptFilter, threshold, ct);
        return Ok(departments);
    }

    /// <summary>
    /// Get all data hygiene alerts (instructor issues, course issues).
    /// </summary>
    [HttpGet("{termCode:int}/hygiene")]
    public async Task<ActionResult<IEnumerable<EffortChangeAlertDto>>> GetDataHygieneAlerts(
        int termCode,
        CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var deptFilter = await GetDepartmentFilterAsync(ct);
        var alerts = await _dashboardService.GetDataHygieneAlertsAsync(termCode, deptFilter, ct);
        return Ok(alerts);
    }

    /// <summary>
    /// Get all data hygiene alerts.
    /// </summary>
    [HttpGet("{termCode:int}/alerts")]
    public async Task<ActionResult<IEnumerable<EffortChangeAlertDto>>> GetAllAlerts(
        int termCode,
        [FromQuery] bool includeIgnored = false,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        var deptFilter = await GetDepartmentFilterAsync(ct);
        var alerts = await _dashboardService.GetDataHygieneAlertsAsync(termCode, deptFilter, ct);

        if (!includeIgnored)
        {
            alerts = alerts.Where(a => a.Status != "Ignored").ToList();
        }

        return Ok(alerts
            .OrderByDescending(a => a.Severity == "High")
            .ThenByDescending(a => a.Severity == "Medium")
            .ThenBy(a => a.EntityName));
    }

    /// <summary>
    /// Get recent changes for a term from the audit log.
    /// Users with ViewAudit or ViewAllDepartments see all changes.
    /// Users with ViewDeptAudit only see changes for their authorized departments.
    /// </summary>
    [HttpGet("{termCode:int}/recent-changes")]
    [Permission(Allow = $"{EffortPermissions.ViewAllDepartments},{EffortPermissions.ViewAudit},{EffortPermissions.ViewDeptAudit}")]
    public async Task<ActionResult<IEnumerable<EffortAuditRow>>> GetRecentChanges(
        int termCode,
        [FromQuery] int limit = 10,
        [FromServices] IEffortAuditService auditService = null!,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        // Get department filtering based on user permissions (same pattern as AuditController)
        var departmentCodes = await GetDepartmentCodesForAuditFilteringAsync(ct);

        // If user has ViewDeptAudit but no authorized departments, deny access
        if (departmentCodes != null && departmentCodes.Count == 0)
        {
            return Forbid();
        }

        var filter = new EffortAuditFilter
        {
            TermCode = termCode,
            ExcludeImports = !await _permissionService.HasPermissionAsync(EffortPermissions.ViewAudit, ct),
            DepartmentCodes = departmentCodes
        };

        var safeLimit = Math.Clamp(limit, 1, 100);
        var recentChanges = await auditService.GetAuditEntriesAsync(
            filter,
            page: 1,
            perPage: safeLimit,
            sortBy: "changedDate",
            descending: true,
            ct);

        return Ok(recentChanges);
    }

    /// <summary>
    /// Get department codes for audit filtering based on user permissions.
    /// Returns null if user has ViewAudit or ViewAllDepartments (no filtering needed).
    /// Returns list of authorized departments for ViewDeptAudit users.
    /// </summary>
    private async Task<List<string>?> GetDepartmentCodesForAuditFilteringAsync(CancellationToken ct)
    {
        // Users with ViewAudit or ViewAllDepartments can see all departments
        if (await _permissionService.HasPermissionAsync(EffortPermissions.ViewAudit, ct) ||
            await _permissionService.HasFullAccessAsync(ct))
        {
            return null;
        }

        var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
        return authorizedDepts.Count == 0 ? [] : authorizedDepts;
    }

    /// <summary>
    /// Ignore an alert (mark as not requiring action).
    /// </summary>
    [HttpPost("{termCode:int}/alerts/ignore")]
    public async Task<ActionResult> IgnoreAlert(
        int termCode,
        [FromBody] IgnoreAlertRequest request,
        CancellationToken ct)
    {
        var alertType = request.AlertType ?? string.Empty;
        var entityId = request.EntityId ?? string.Empty;

        SetExceptionContext("termCode", termCode);
        SetExceptionContext("alertType", LogSanitizer.SanitizeString(alertType) ?? string.Empty);
        SetExceptionContext("entityId", LogSanitizer.SanitizeString(entityId) ?? string.Empty);

        var currentUser = new UserHelper().GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var deptFilter = await GetDepartmentFilterAsync(ct);
        var success = await _dashboardService.IgnoreAlertAsync(
            termCode,
            alertType,
            entityId,
            currentUser.AaudUserId,
            deptFilter,
            ct);

        if (!success)
        {
            return NotFound("Alert not found");
        }

        _logger.LogInformation("Alert ignored: {AlertType}/{EntityId}",
            LogSanitizer.SanitizeString(alertType),
            LogSanitizer.SanitizeString(entityId));
        return NoContent();
    }
}

/// <summary>
/// Request model for ignoring an alert.
/// </summary>
public class IgnoreAlertRequest
{
    /// <summary>
    /// Alert type (e.g., NoRecords, ZeroHours).
    /// </summary>
    public string AlertType { get; set; } = string.Empty;

    /// <summary>
    /// Entity identifier (PersonId or CourseId).
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
}
