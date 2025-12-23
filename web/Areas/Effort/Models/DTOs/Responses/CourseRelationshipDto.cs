namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for course relationship data.
/// </summary>
public class CourseRelationshipDto
{
    public int Id { get; set; }
    public int ParentCourseId { get; set; }
    public int ChildCourseId { get; set; }

    /// <summary>
    /// Relationship type: "CrossList" or "Section"
    /// </summary>
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>
    /// Child course details (populated when viewing a parent's children).
    /// </summary>
    public CourseDto? ChildCourse { get; set; }

    /// <summary>
    /// Parent course details (populated when viewing a child's parent).
    /// </summary>
    public CourseDto? ParentCourse { get; set; }
}
