namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for instructor effort records with course information.
/// Used for the instructor detail page.
/// </summary>
public class InstructorEffortRecordDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int PersonId { get; set; }
    public int TermCode { get; set; }
    public string EffortType { get; set; } = string.Empty;
    public string EffortTypeDescription { get; set; } = string.Empty;
    public int Role { get; set; }
    public string RoleDescription { get; set; } = string.Empty;
    public int? Hours { get; set; }
    public int? Weeks { get; set; }
    public string Crn { get; set; } = string.Empty;
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
    /// Course information for this effort record.
    /// </summary>
    public CourseDto Course { get; set; } = null!;

    /// <summary>
    /// Child courses (cross-listed or sectioned) for this course.
    /// Only populated for parent courses that have children.
    /// </summary>
    public List<ChildCourseDto> ChildCourses { get; set; } = new();
}

/// <summary>
/// Simplified DTO for child course display in instructor effort records.
/// </summary>
public class ChildCourseDto
{
    public int Id { get; set; }
    public string SubjCode { get; set; } = string.Empty;
    public string CrseNumb { get; set; } = string.Empty;
    public string SeqNumb { get; set; } = string.Empty;
    public decimal Units { get; set; }
    public int Enrollment { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
}
