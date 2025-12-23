using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service interface for course relationship operations.
/// </summary>
public interface ICourseRelationshipService
{
    /// <summary>
    /// Get all relationships for a course (both as parent and child).
    /// </summary>
    Task<CourseRelationshipsResult> GetRelationshipsForCourseAsync(int courseId, CancellationToken ct = default);

    /// <summary>
    /// Get child relationships for a parent course.
    /// </summary>
    Task<List<CourseRelationshipDto>> GetChildRelationshipsAsync(int parentCourseId, CancellationToken ct = default);

    /// <summary>
    /// Get the parent relationship for a child course (if any).
    /// </summary>
    Task<CourseRelationshipDto?> GetParentRelationshipAsync(int childCourseId, CancellationToken ct = default);

    /// <summary>
    /// Get courses available to be linked as children of a parent course.
    /// Excludes: the parent itself, courses already linked as children, and courses that are already children of another parent.
    /// </summary>
    Task<List<CourseDto>> GetAvailableChildCoursesAsync(int parentCourseId, CancellationToken ct = default);

    /// <summary>
    /// Create a new course relationship.
    /// </summary>
    Task<CourseRelationshipDto> CreateRelationshipAsync(int parentCourseId, CreateCourseRelationshipRequest request, CancellationToken ct = default);

    /// <summary>
    /// Delete a course relationship.
    /// </summary>
    Task<bool> DeleteRelationshipAsync(int relationshipId, CancellationToken ct = default);

    /// <summary>
    /// Get a relationship by ID.
    /// </summary>
    Task<CourseRelationshipDto?> GetRelationshipAsync(int relationshipId, CancellationToken ct = default);
}

/// <summary>
/// Result containing both parent and child relationships for a course.
/// </summary>
public class CourseRelationshipsResult
{
    /// <summary>
    /// If this course is a child, the parent relationship.
    /// </summary>
    public CourseRelationshipDto? ParentRelationship { get; set; }

    /// <summary>
    /// If this course is a parent, the list of child relationships.
    /// </summary>
    public List<CourseRelationshipDto> ChildRelationships { get; set; } = new();
}
