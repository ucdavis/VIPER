namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for effort records displayed on the course detail page.
/// Groups effort by instructor with per-record details.
/// </summary>
public class CourseEffortRecordDto
{
    public int EffortId { get; set; }
    public int PersonId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string EffortTypeId { get; set; } = string.Empty;
    public string EffortTypeDescription { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleDescription { get; set; } = string.Empty;
    public int? Hours { get; set; }
    public int? Weeks { get; set; }
    public string? Notes { get; set; }
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Effort value - either hours or weeks depending on effort type.
    /// </summary>
    public int? EffortValue => Hours ?? Weeks;

    /// <summary>
    /// Label for the effort value ("hours" or "weeks").
    /// </summary>
    public string EffortLabel => Hours.HasValue ? "hours" : "weeks";

    /// <summary>
    /// Whether the current user can edit this record.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// Whether the current user can delete this record.
    /// </summary>
    public bool CanDelete { get; set; }
}

/// <summary>
/// Response wrapper for course effort data including the records and course context.
/// </summary>
public class CourseEffortResponseDto
{
    /// <summary>
    /// The course ID.
    /// </summary>
    public int CourseId { get; set; }

    /// <summary>
    /// Term code for the course.
    /// </summary>
    public int TermCode { get; set; }

    /// <summary>
    /// Whether the current user can add new effort records to this course.
    /// </summary>
    public bool CanAddEffort { get; set; }

    /// <summary>
    /// Whether this is a child course (effort should be added on parent).
    /// </summary>
    public bool IsChildCourse { get; set; }

    /// <summary>
    /// Effort records for this course.
    /// </summary>
    public List<CourseEffortRecordDto> Records { get; set; } = new();
}
