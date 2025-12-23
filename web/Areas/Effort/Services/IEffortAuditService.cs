using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for logging and querying audit trail entries in the Effort system.
/// </summary>
public interface IEffortAuditService
{
    Task LogPercentageChangeAsync(int percentageId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default);

    Task LogRecordChangeAsync(int recordId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default);

    Task LogPersonChangeAsync(int personId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default);

    Task LogCourseChangeAsync(int courseId, int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default);

    Task LogTermChangeAsync(int termCode, string action,
        object? oldValues, object? newValues, CancellationToken ct = default);

    Task LogImportAsync(int termCode, string action, string details, CancellationToken ct = default);

    /// <summary>
    /// Add a term change audit entry to the context without saving.
    /// Use this within a transaction where the caller manages SaveChangesAsync.
    /// </summary>
    void AddTermChangeAudit(int termCode, string action, object? oldValues, object? newValues);

    /// <summary>
    /// Add a course change audit entry to the context without saving.
    /// Use this within a transaction where the caller manages SaveChangesAsync.
    /// </summary>
    void AddCourseChangeAudit(int courseId, int termCode, string action, object? oldValues, object? newValues);

    /// <summary>
    /// Add an import audit entry to the context without saving.
    /// Use this within a transaction where the caller manages SaveChangesAsync.
    /// </summary>
    void AddImportAudit(int termCode, string action, string details);

    /// <summary>
    /// Add a person (instructor) change audit entry to the context without saving.
    /// Use this within a transaction where the caller manages SaveChangesAsync.
    /// </summary>
    void AddPersonChangeAudit(int personId, int termCode, string action, object? oldValues, object? newValues);

    // ==================== Query Methods ====================

    /// <summary>
    /// Get paginated audit entries with optional filtering.
    /// </summary>
    /// <param name="filter">Filter parameters.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="perPage">Records per page.</param>
    /// <param name="sortBy">Column to sort by.</param>
    /// <param name="descending">Sort descending if true.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of audit entries.</returns>
    Task<List<EffortAuditRow>> GetAuditEntriesAsync(EffortAuditFilter filter,
        int page, int perPage, string? sortBy, bool descending, CancellationToken ct = default);

    /// <summary>
    /// Get total count of audit entries matching the filter.
    /// </summary>
    /// <param name="filter">Filter parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Total count of matching entries.</returns>
    Task<int> GetAuditEntryCountAsync(EffortAuditFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Get distinct action types from audit entries.
    /// </summary>
    /// <param name="excludeImports">If true, excludes import-related actions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of distinct action strings.</returns>
    Task<List<string>> GetDistinctActionsAsync(bool excludeImports, CancellationToken ct = default);

    /// <summary>
    /// Get users who have made audit entries (for modifier filter).
    /// </summary>
    /// <param name="excludeImports">If true, excludes modifiers who only appear in import actions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of modifier info.</returns>
    Task<List<ModifierInfo>> GetDistinctModifiersAsync(bool excludeImports = false, CancellationToken ct = default);

    /// <summary>
    /// Get terms that have audit entries (for term filter).
    /// </summary>
    /// <param name="excludeImports">If true, excludes terms that only appear in import actions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of term codes with audit entries.</returns>
    Task<List<int>> GetAuditTermCodesAsync(bool excludeImports = false, CancellationToken ct = default);

    /// <summary>
    /// Get instructors who have audit entries (for instructor filter).
    /// </summary>
    /// <param name="excludeImports">If true, excludes instructors who only appear in import actions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of instructor info.</returns>
    Task<List<ModifierInfo>> GetDistinctInstructorsAsync(bool excludeImports = false, CancellationToken ct = default);

    /// <summary>
    /// Get distinct subject codes from courses.
    /// </summary>
    /// <param name="termCode">Optional term code to filter by.</param>
    /// <param name="courseNumber">Optional course number to filter by.</param>
    /// <param name="excludeImports">If true, excludes subject codes that only appear in import actions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of distinct subject codes.</returns>
    Task<List<string>> GetDistinctSubjectCodesAsync(int? termCode = null, string? courseNumber = null, bool excludeImports = false, CancellationToken ct = default);

    /// <summary>
    /// Get distinct course numbers from courses.
    /// </summary>
    /// <param name="termCode">Optional term code to filter by.</param>
    /// <param name="subjectCode">Optional subject code to filter by.</param>
    /// <param name="excludeImports">If true, excludes course numbers that only appear in import actions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of distinct course numbers.</returns>
    Task<List<string>> GetDistinctCourseNumbersAsync(int? termCode = null, string? subjectCode = null, bool excludeImports = false, CancellationToken ct = default);
}
