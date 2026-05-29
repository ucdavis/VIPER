namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents relationships between courses (parent/child, cross-list, sections).
/// Maps to effort.CourseRelationships table.
/// </summary>
public class CourseRelationship
{
    public int Id { get; set; }
    public int ParentCourseId { get; set; }
    public int ChildCourseId { get; set; }
    public string RelationshipType { get; set; } = string.Empty;

    // Navigation properties
    public virtual EffortCourse ParentCourse { get; set; } = null!;
    public virtual EffortCourse ChildCourse { get; set; } = null!;
}
