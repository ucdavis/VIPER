using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for course-related operations in the Effort system.
/// </summary>
public interface ICourseService
{
    /// <summary>
    /// Get all courses for a term, optionally filtered by department.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="department">Optional department filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of courses.</returns>
    Task<List<CourseDto>> GetCoursesAsync(int termCode, string? department = null, CancellationToken ct = default);

    /// <summary>
    /// Get a single course by ID.
    /// </summary>
    /// <param name="courseId">The course ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The course, or null if not found.</returns>
    Task<CourseDto?> GetCourseAsync(int courseId, CancellationToken ct = default);

    /// <summary>
    /// Search for courses in Banner by subject code, course number, sequence number, and/or CRN.
    /// Queries Banner directly via linked server stored procedure.
    /// At least one search parameter is required.
    /// </summary>
    /// <param name="termCode">The term code to search in.</param>
    /// <param name="subjCode">Optional subject code filter (e.g., "VME", "PHI").</param>
    /// <param name="crseNumb">Optional course number filter (e.g., "443").</param>
    /// <param name="seqNumb">Optional sequence number filter (e.g., "001").</param>
    /// <param name="crn">Optional CRN filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of Banner courses matching the criteria.</returns>
    /// <exception cref="ArgumentException">Thrown if no search parameters are provided.</exception>
    Task<List<BannerCourseDto>> SearchBannerCoursesAsync(int termCode, string? subjCode = null,
        string? crseNumb = null, string? seqNumb = null, string? crn = null, CancellationToken ct = default);

    /// <summary>
    /// Get a single Banner course by term code and CRN.
    /// Used by the controller to validate before import.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="crn">The course reference number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Banner course, or null if not found.</returns>
    Task<BannerCourseDto?> GetBannerCourseAsync(int termCode, string crn, CancellationToken ct = default);

    /// <summary>
    /// Check if a course with the given key already exists in the Effort system.
    /// Used by the controller to check for duplicates before import/create.
    /// </summary>
    /// <param name="termCode">The term code.</param>
    /// <param name="crn">The course reference number.</param>
    /// <param name="units">The unit value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a course with this key exists, false otherwise.</returns>
    Task<bool> CourseExistsAsync(int termCode, string crn, decimal units, CancellationToken ct = default);

    /// <summary>
    /// Import a course from Banner into the Effort system.
    /// The controller should validate the Banner course exists and check for duplicates before calling this.
    /// </summary>
    /// <param name="request">The import request with term code, CRN, and optional units.</param>
    /// <param name="bannerCourse">The pre-fetched Banner course data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created course.</returns>
    Task<CourseDto> ImportCourseFromBannerAsync(ImportCourseRequest request, BannerCourseDto bannerCourse, CancellationToken ct = default);

    /// <summary>
    /// Manually create a course in the Effort system (for courses not in Banner).
    /// </summary>
    /// <param name="request">The course creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created course.</returns>
    Task<CourseDto> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update an existing course (full update).
    /// </summary>
    /// <param name="courseId">The course ID to update.</param>
    /// <param name="request">The update request with new values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated course, or null if not found.</returns>
    Task<CourseDto?> UpdateCourseAsync(int courseId, UpdateCourseRequest request, CancellationToken ct = default);

    /// <summary>
    /// Update only the enrollment for an R-course.
    /// This enforces that only R-courses (course number ending with 'R') can be updated.
    /// </summary>
    /// <param name="courseId">The course ID to update.</param>
    /// <param name="enrollment">The new enrollment value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated course, or null if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the course is not an R-course.</exception>
    Task<CourseDto?> UpdateCourseEnrollmentAsync(int courseId, int enrollment, CancellationToken ct = default);

    /// <summary>
    /// Delete a course and all associated effort records.
    /// </summary>
    /// <param name="courseId">The course ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteCourseAsync(int courseId, CancellationToken ct = default);

    /// <summary>
    /// Get deletion info for a course. Returns record count so the UI can warn
    /// the user about associated effort records that will be cascade-deleted.
    /// Users with DeleteCourse permission can always delete.
    /// </summary>
    /// <param name="courseId">The course ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple with canDelete flag (always true for authorized users) and count of associated effort records for UI warning.</returns>
    Task<(bool CanDelete, int RecordCount)> CanDeleteCourseAsync(int courseId, CancellationToken ct = default);

    /// <summary>
    /// Get the list of valid custodial department codes.
    /// </summary>
    /// <returns>List of valid department codes.</returns>
    List<string> GetValidCustodialDepartments();

    /// <summary>
    /// Check if a custodial department code is valid.
    /// </summary>
    /// <param name="departmentCode">The department code to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValidCustodialDepartment(string departmentCode);

    /// <summary>
    /// Get the custodial department code that would be assigned for a Banner department code.
    /// Used by the controller to check authorization before import.
    /// </summary>
    /// <param name="bannerDeptCode">The Banner department code.</param>
    /// <returns>The mapped custodial department code.</returns>
    string GetCustodialDepartmentForBannerCode(string bannerDeptCode);
}
