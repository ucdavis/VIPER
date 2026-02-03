namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Alert for data hygiene issues in the Effort system.
/// </summary>
public class EffortChangeAlertDto
{
    /// <summary>
    /// Alert type: NoRecords, NoInstructors, NoDepartment, ZeroHours, NotVerified
    /// </summary>
    public string AlertType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable alert title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Entity type affected: Instructor, Course, Department
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Entity identifier (PersonId, CourseId, DeptCode).
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Entity name for display.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Department code (for filtering).
    /// </summary>
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Severity level: High, Medium, Low
    /// </summary>
    public string Severity { get; set; } = "Medium";

    /// <summary>
    /// Alert status: Active, Resolved, Ignored
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Whether this alert was automatically resolved (issue no longer exists).
    /// </summary>
    public bool IsResolved => Status == "Resolved";

    /// <summary>
    /// Whether this alert was manually ignored by a user.
    /// </summary>
    public bool IsIgnored => Status == "Ignored";

    /// <summary>
    /// When the alert was ignored (if applicable).
    /// </summary>
    public DateTime? ReviewedDate { get; set; }
}
