using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for course relationship operations (cross-listing and sections).
/// </summary>
[Route("/api/effort/courses/{parentCourseId:int}/relationships")]
public class CourseRelationshipsController : BaseEffortController
{
    private readonly ICourseRelationshipService _relationshipService;
    private readonly ICourseService _courseService;
    private readonly IEffortPermissionService _permissionService;

    public CourseRelationshipsController(
        ICourseRelationshipService relationshipService,
        ICourseService courseService,
        IEffortPermissionService permissionService,
        ILogger<CourseRelationshipsController> logger) : base(logger)
    {
        _relationshipService = relationshipService;
        _courseService = courseService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Verifies the user is authorized to access a course by its department.
    /// Returns null if authorized, or a NotFound result if not.
    /// </summary>
    private async Task<(CourseDto? course, ActionResult? errorResult)> GetAuthorizedCourseAsync(int courseId, CancellationToken ct)
    {
        var course = await _courseService.GetCourseAsync(courseId, ct);
        if (course == null || !await _permissionService.CanViewDepartmentAsync(course.CustDept, ct))
        {
            return (null, NotFound($"Course {courseId} not found"));
        }
        return (course, null);
    }

    /// <summary>
    /// Get all relationships for a course (both parent and child relationships).
    /// </summary>
    [HttpGet]
    [Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
    public async Task<ActionResult<CourseRelationshipsResult>> GetRelationships(int parentCourseId, CancellationToken ct = default)
    {
        SetExceptionContext("courseId", parentCourseId);

        var (_, errorResult) = await GetAuthorizedCourseAsync(parentCourseId, ct);
        if (errorResult != null) return errorResult;

        var result = await _relationshipService.GetRelationshipsForCourseAsync(parentCourseId, ct);

        // Filter relationships to only those the user can access
        var hasFullAccess = await _permissionService.HasFullAccessAsync(ct);
        if (!hasFullAccess)
        {
            var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
            result.ChildRelationships = result.ChildRelationships
                .Where(r => r.ChildCourse != null &&
                    authorizedDepts.Contains(r.ChildCourse.CustDept, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (result.ParentRelationship?.ParentCourse != null &&
                !authorizedDepts.Contains(result.ParentRelationship.ParentCourse.CustDept, StringComparer.OrdinalIgnoreCase))
            {
                result.ParentRelationship = null;
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Get courses available to be linked as children of the specified parent course.
    /// </summary>
    [HttpGet("available-children")]
    [Permission(Allow = EffortPermissions.LinkCourses)]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAvailableChildren(int parentCourseId, CancellationToken ct = default)
    {
        SetExceptionContext("courseId", parentCourseId);

        var (_, errorResult) = await GetAuthorizedCourseAsync(parentCourseId, ct);
        if (errorResult != null) return errorResult;

        var availableCourses = await _relationshipService.GetAvailableChildCoursesAsync(parentCourseId, ct);

        // Filter to courses the user can access
        var hasFullAccess = await _permissionService.HasFullAccessAsync(ct);
        if (!hasFullAccess)
        {
            var authorizedDepts = await _permissionService.GetAuthorizedDepartmentsAsync(ct);
            availableCourses = availableCourses
                .Where(c => authorizedDepts.Contains(c.CustDept, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return Ok(availableCourses);
    }

    /// <summary>
    /// Create a new course relationship.
    /// </summary>
    [HttpPost]
    [Permission(Allow = EffortPermissions.LinkCourses)]
    public async Task<ActionResult<CourseRelationshipDto>> CreateRelationship(
        int parentCourseId,
        [FromBody] CreateCourseRelationshipRequest request,
        CancellationToken ct = default)
    {
        SetExceptionContext("parentCourseId", parentCourseId);
        SetExceptionContext("childCourseId", request.ChildCourseId);

        // Verify access to parent course
        var (_, errorResult) = await GetAuthorizedCourseAsync(parentCourseId, ct);
        if (errorResult != null) return errorResult;

        // Verify access to child course
        var (_, childErrorResult) = await GetAuthorizedCourseAsync(request.ChildCourseId, ct);
        if (childErrorResult != null) return childErrorResult;

        try
        {
            var relationship = await _relationshipService.CreateRelationshipAsync(parentCourseId, request, ct);
            _logger.LogInformation("Created {Type} relationship: course {ParentId} -> {ChildId}",
                request.RelationshipType, parentCourseId, request.ChildCourseId);

            return CreatedAtAction(nameof(GetRelationships), new { parentCourseId }, relationship);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create relationship: {Message}", LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Delete a course relationship.
    /// </summary>
    [HttpDelete("{relationshipId:int}")]
    [Permission(Allow = EffortPermissions.LinkCourses)]
    public async Task<ActionResult> DeleteRelationship(int parentCourseId, int relationshipId, CancellationToken ct = default)
    {
        SetExceptionContext("parentCourseId", parentCourseId);
        SetExceptionContext("relationshipId", relationshipId);

        // Get the relationship to verify it belongs to the parent course
        var relationship = await _relationshipService.GetRelationshipAsync(relationshipId, ct);
        if (relationship == null)
        {
            return NotFound($"Relationship {relationshipId} not found");
        }

        if (relationship.ParentCourseId != parentCourseId)
        {
            return NotFound($"Relationship {relationshipId} not found for course {parentCourseId}");
        }

        // Verify access to parent course
        var (_, errorResult) = await GetAuthorizedCourseAsync(parentCourseId, ct);
        if (errorResult != null) return errorResult;

        // Verify access to child course
        var (_, childErrorResult) = await GetAuthorizedCourseAsync(relationship.ChildCourseId, ct);
        if (childErrorResult != null) return childErrorResult;

        var deleted = await _relationshipService.DeleteRelationshipAsync(relationshipId, ct);
        if (!deleted)
        {
            return NotFound($"Relationship {relationshipId} not found");
        }

        _logger.LogInformation("Deleted relationship {RelationshipId}", relationshipId);
        return NoContent();
    }
}
