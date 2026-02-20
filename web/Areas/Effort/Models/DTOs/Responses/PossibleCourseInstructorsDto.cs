namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for possible instructors when adding effort from the course page.
/// Groups instructors into those already on the course and all other available instructors.
/// </summary>
public class PossibleCourseInstructorsDto
{
    /// <summary>
    /// Instructors who already have effort records on this course.
    /// </summary>
    public List<CourseInstructorOptionDto> ExistingInstructors { get; set; } = new();

    /// <summary>
    /// All instructors for the term who do not yet have effort on this course.
    /// Guest accounts are excluded.
    /// </summary>
    public List<CourseInstructorOptionDto> OtherInstructors { get; set; } = new();
}

/// <summary>
/// DTO for an instructor option in the course effort add dialog.
/// </summary>
public class CourseInstructorOptionDto
{
    public int PersonId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{LastName}, {FirstName}";
    public string EffortDept { get; set; } = string.Empty;
}
