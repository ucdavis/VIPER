using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for dashboard statistics and data hygiene operations.
/// All methods accept an optional departmentCodes filter:
/// - null/empty = all departments (for admins with ViewAllDepartments)
/// - populated = filter to specified departments (for users with ViewDept)
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get dashboard statistics for a term.
    /// </summary>
    /// <param name="termCode">Term code to get stats for.</param>
    /// <param name="departmentCodes">Optional department filter. Null/empty = all departments.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<DashboardStatsDto> GetDashboardStatsAsync(int termCode, List<string>? departmentCodes = null, CancellationToken ct = default);

    /// <summary>
    /// Get verification breakdown by department for a term.
    /// </summary>
    /// <param name="termCode">Term code to get stats for.</param>
    /// <param name="departmentCodes">Optional department filter. Null/empty = all departments.</param>
    /// <param name="threshold">Verification threshold percentage (default 80).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<DepartmentVerificationDto>> GetDepartmentVerificationAsync(int termCode, List<string>? departmentCodes = null, int threshold = 80, CancellationToken ct = default);

    /// <summary>
    /// Get all data hygiene alerts (instructor issues, course issues) for a term.
    /// </summary>
    /// <param name="termCode">Term code to get alerts for.</param>
    /// <param name="departmentCodes">Optional department filter. Null/empty = all departments.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<List<EffortChangeAlertDto>> GetDataHygieneAlertsAsync(int termCode, List<string>? departmentCodes = null, CancellationToken ct = default);

    /// <summary>
    /// Ignore an alert (mark as not requiring action).
    /// </summary>
    /// <param name="termCode">Term code.</param>
    /// <param name="alertType">Alert type (e.g., NoRecords, ZeroHours).</param>
    /// <param name="entityId">Entity identifier (PersonId or CourseId).</param>
    /// <param name="ignoredBy">PersonId of the user ignoring the alert.</param>
    /// <param name="departmentCodes">Department filter for authorization. Null = full access, empty = no access.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> IgnoreAlertAsync(int termCode, string alertType, string entityId, int ignoredBy, List<string>? departmentCodes = null, CancellationToken ct = default);
}
