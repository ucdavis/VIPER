using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes;
using Viper.Classes.Utilities;
using Viper.Models;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// Controller for viewing effort system audit trail.
/// </summary>
[Route("/api/effort/audit")]
[Permission(Allow = EffortPermissions.ViewAudit)]
public class AuditController : BaseEffortController
{
    private readonly IEffortAuditService _auditService;
    private readonly IEffortPermissionService _permissionService;

    public AuditController(
        IEffortAuditService auditService,
        IEffortPermissionService permissionService,
        ILogger<AuditController> logger)
        : base(logger)
    {
        _auditService = auditService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Determines if import-related audit entries should be excluded based on user permissions.
    /// Users without full access cannot see import actions.
    /// </summary>
    private async Task<bool> ShouldExcludeImportsAsync(CancellationToken ct)
        => !await _permissionService.HasFullAccessAsync(ct);

    /// <summary>
    /// Get paginated audit entries with optional filtering.
    /// </summary>
    [HttpGet]
    [ApiPagination(DefaultPerPage = 25, MaxPerPage = 500)]
    public async Task<ActionResult<List<EffortAuditRow>>> GetAudit(
        int? termCode,
        string? action,
        int? instructorPersonId,
        int? modifiedByPersonId,
        string? searchText,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? subjectCode,
        string? courseNumber,
        ApiPagination? pagination,
        string? sortBy = "changedDate",
        bool descending = true,
        CancellationToken ct = default)
    {
        SetExceptionContext(nameof(GetAudit), new
        {
            termCode,
            action = LogSanitizer.SanitizeString(action),
            instructorPersonId,
            modifiedByPersonId,
            searchText = LogSanitizer.SanitizeString(searchText),
            dateFrom,
            dateTo,
            subjectCode = LogSanitizer.SanitizeString(subjectCode),
            courseNumber = LogSanitizer.SanitizeString(courseNumber)
        });

        var filter = new EffortAuditFilter
        {
            TermCode = termCode,
            Action = action,
            InstructorPersonId = instructorPersonId,
            ModifiedByPersonId = modifiedByPersonId,
            SearchText = searchText,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SubjectCode = subjectCode,
            CourseNumber = courseNumber,
            ExcludeImports = await ShouldExcludeImportsAsync(ct)
        };

        var page = pagination?.Page ?? 1;
        var perPage = pagination?.PerPage ?? 25;

        var results = await _auditService.GetAuditEntriesAsync(filter, page, perPage, sortBy, descending, ct);

        if (pagination != null)
        {
            pagination.TotalRecords = await _auditService.GetAuditEntryCountAsync(filter, ct);
        }

        return results;
    }

    /// <summary>
    /// Get distinct audit actions for filter dropdown.
    /// </summary>
    [HttpGet("actions")]
    public async Task<ActionResult<List<string>>> GetAuditActions(CancellationToken ct = default)
    {
        SetExceptionContext("Method", nameof(GetAuditActions));

        return await _auditService.GetDistinctActionsAsync(await ShouldExcludeImportsAsync(ct), ct);
    }

    /// <summary>
    /// Get users who have made audit entries for filter dropdown.
    /// </summary>
    [HttpGet("modifiers")]
    public async Task<ActionResult<List<ModifierInfo>>> GetModifiers(CancellationToken ct = default)
    {
        SetExceptionContext("Method", nameof(GetModifiers));

        return await _auditService.GetDistinctModifiersAsync(await ShouldExcludeImportsAsync(ct), ct);
    }

    /// <summary>
    /// Get instructors who have audit entries for filter dropdown.
    /// </summary>
    [HttpGet("instructors")]
    public async Task<ActionResult<List<ModifierInfo>>> GetInstructors(CancellationToken ct = default)
    {
        SetExceptionContext("Method", nameof(GetInstructors));

        return await _auditService.GetDistinctInstructorsAsync(await ShouldExcludeImportsAsync(ct), ct);
    }

    /// <summary>
    /// Get terms that have audit entries for filter dropdown.
    /// </summary>
    [HttpGet("terms")]
    public async Task<ActionResult<List<TermOptionDto>>> GetAuditTerms(CancellationToken ct = default)
    {
        SetExceptionContext("Method", nameof(GetAuditTerms));

        var termCodes = await _auditService.GetAuditTermCodesAsync(await ShouldExcludeImportsAsync(ct), ct);

        return termCodes.Select(tc =>
        {
            var termName = TermCodeService.GetTermCodeDescription(tc);
            return new TermOptionDto
            {
                TermCode = tc,
                TermName = termName.StartsWith("Unknown Term") ? $"Term {tc}" : termName
            };
        }).ToList();
    }

    /// <summary>
    /// Get distinct subject codes for filter dropdown.
    /// Optionally filtered by term code and/or course number.
    /// </summary>
    [HttpGet("subject-codes")]
    public async Task<ActionResult<List<string>>> GetSubjectCodes(int? termCode = null, string? courseNumber = null, CancellationToken ct = default)
    {
        SetExceptionContext("Method", nameof(GetSubjectCodes));

        return await _auditService.GetDistinctSubjectCodesAsync(termCode, courseNumber, await ShouldExcludeImportsAsync(ct), ct);
    }

    /// <summary>
    /// Get distinct course numbers for filter dropdown.
    /// Optionally filtered by term code and/or subject code.
    /// </summary>
    [HttpGet("course-numbers")]
    public async Task<ActionResult<List<string>>> GetCourseNumbers(int? termCode = null, string? subjectCode = null, CancellationToken ct = default)
    {
        SetExceptionContext("Method", nameof(GetCourseNumbers));

        return await _auditService.GetDistinctCourseNumbersAsync(termCode, subjectCode, await ShouldExcludeImportsAsync(ct), ct);
    }
}

/// <summary>
/// Simple DTO for term dropdown options in audit filter.
/// </summary>
public class TermOptionDto
{
    public int TermCode { get; set; }
    public string TermName { get; set; } = string.Empty;
}
