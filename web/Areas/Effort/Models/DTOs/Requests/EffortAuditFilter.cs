namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Filter parameters for querying audit entries.
/// </summary>
public class EffortAuditFilter
{
    /// <summary>
    /// Filter by term code.
    /// </summary>
    public int? TermCode { get; set; }

    /// <summary>
    /// Filter by action type (e.g., CreatePercent, UpdateRecord).
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter by the instructor/person affected by the change.
    /// </summary>
    public int? InstructorPersonId { get; set; }

    /// <summary>
    /// Filter by the user who made the change.
    /// </summary>
    public int? ModifiedByPersonId { get; set; }

    /// <summary>
    /// Search text to match against the Changes field.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Filter changes on or after this date.
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Filter changes on or before this date.
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Filter by course subject code (e.g., VMD, VET).
    /// </summary>
    public string? SubjectCode { get; set; }

    /// <summary>
    /// Filter by course number.
    /// </summary>
    public string? CourseNumber { get; set; }

    /// <summary>
    /// When true, excludes import-related actions from results.
    /// </summary>
    public bool ExcludeImports { get; set; }

    /// <summary>
    /// Filter by department codes. Used for department-level audit access.
    /// When set, only shows audit entries related to the specified departments.
    /// </summary>
    public List<string>? DepartmentCodes { get; set; }
}
