using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for instructor-related operations in the Effort system.
/// </summary>
public interface IInstructorService
{
    /// <summary>
    /// Get all instructors for a term, optionally filtered by department.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="department">Optional department filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of instructors.</returns>
    Task<List<PersonDto>> GetInstructorsAsync(int termCode, string? department = null, CancellationToken ct = default);

    /// <summary>
    /// Get all instructors for a term filtered by multiple departments.
    /// Uses a single query with IN clause for better performance.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="departments">List of department codes to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of instructors sorted by last name, first name.</returns>
    Task<List<PersonDto>> GetInstructorsByDepartmentsAsync(int termCode, IReadOnlyList<string> departments, CancellationToken ct = default);

    /// <summary>
    /// Get a single instructor by person ID and term code.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The instructor, or null if not found.</returns>
    Task<PersonDto?> GetInstructorAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Search for possible instructors in AAUD who are not already in the Effort system for the term.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="searchTerm">Optional search term to filter by name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of AAUD persons who can be added as instructors.</returns>
    Task<List<AaudPersonDto>> SearchPossibleInstructorsAsync(int termCode, string? searchTerm = null, CancellationToken ct = default);

    /// <summary>
    /// Add an instructor to the Effort system for a term.
    /// Instructor info is looked up from AAUD.
    /// </summary>
    /// <param name="request">The creation request with PersonId and TermCode.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created instructor.</returns>
    /// <exception cref="Exceptions.InstructorAlreadyExistsException">
    /// Thrown when an instructor with the same PersonId already exists for the specified term.
    /// </exception>
    Task<PersonDto> CreateInstructorAsync(CreateInstructorRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an instructor's details.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated instructor, or null if not found.</returns>
    Task<PersonDto?> UpdateInstructorAsync(int personId, int termCode, UpdateInstructorRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete an instructor and all associated effort records for the term.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteInstructorAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get deletion info for an instructor. Returns record count so the UI can warn
    /// the user about associated effort records that will be cascade-deleted.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with canDelete flag and count of associated effort records.</returns>
    Task<(bool CanDelete, int RecordCount)> CanDeleteInstructorAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Check if an instructor already exists for a term.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the instructor exists for this term.</returns>
    Task<bool> InstructorExistsAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Resolve the department a person would be assigned to if added as an instructor.
    /// Used for authorization checks before creating.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code for AAUD lookup.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved department code, or null if person not found.</returns>
    Task<string?> ResolveInstructorDepartmentAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get the list of valid department codes with grouping information.
    /// </summary>
    /// <returns>List of departments with code, name, and group.</returns>
    List<DepartmentDto> GetDepartments();

    /// <summary>
    /// Get the list of valid department codes.
    /// </summary>
    /// <returns>List of valid department codes.</returns>
    List<string> GetValidDepartments();

    /// <summary>
    /// Check if a department code is valid.
    /// </summary>
    /// <param name="departmentCode">The department code to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValidDepartment(string departmentCode);

    /// <summary>
    /// Get all report units for the multi-select dropdown.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of report units.</returns>
    Task<List<ReportUnitDto>> GetReportUnitsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get effort records for an instructor in a specific term, including course information.
    /// </summary>
    /// <param name="personId">The person ID.</param>
    /// <param name="termCode">The term code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of effort records with course info.</returns>
    Task<List<InstructorEffortRecordDto>> GetInstructorEffortRecordsAsync(int personId, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get all title codes from the dictionary database for the dropdown.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of title codes with human-readable names.</returns>
    Task<List<TitleCodeDto>> GetTitleCodesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get all job groups currently in use by instructors for the dropdown.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of job groups with human-readable names.</returns>
    Task<List<JobGroupDto>> GetJobGroupsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a cached lookup of raw department codes to simple names from the dictionary database.
    /// Maps codes like "072030" to simple names like "VME".
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping raw codes to simple names, or null if unavailable.</returns>
    Task<Dictionary<string, string>?> GetDepartmentSimpleNameLookupAsync(CancellationToken ct = default);

    /// <summary>
    /// Resolve departments for multiple instructors in a single batch.
    /// Uses the full resolution chain (override → jobs → employee fields → fallback).
    /// </summary>
    /// <param name="mothraIds">List of MothraIDs to resolve.</param>
    /// <param name="termCode">The term code for AAUD lookup.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping MothraID to resolved department code (at least "UNK").</returns>
    Task<Dictionary<string, string>> BatchResolveDepartmentsAsync(
        List<string> mothraIds, int termCode, CancellationToken ct = default);
}
