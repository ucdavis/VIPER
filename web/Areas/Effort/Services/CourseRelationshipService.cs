using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for course relationship operations (cross-listing and sections).
/// </summary>
public class CourseRelationshipService : ICourseRelationshipService
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditService;
    private readonly ILogger<CourseRelationshipService> _logger;

    public CourseRelationshipService(
        EffortDbContext context,
        IEffortAuditService auditService,
        ILogger<CourseRelationshipService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CourseRelationshipsResult> GetRelationshipsForCourseAsync(int courseId, CancellationToken ct = default)
    {
        var result = new CourseRelationshipsResult();

        // Check if this course is a child of another course
        result.ParentRelationship = await GetParentRelationshipAsync(courseId, ct);

        // Get children of this course
        result.ChildRelationships = await GetChildRelationshipsAsync(courseId, ct);

        return result;
    }

    public async Task<List<CourseRelationshipDto>> GetChildRelationshipsAsync(int parentCourseId, CancellationToken ct = default)
    {
        var relationships = await _context.CourseRelationships
            .AsNoTracking()
            .Include(r => r.ChildCourse)
            .Where(r => r.ParentCourseId == parentCourseId)
            .OrderBy(r => r.ChildCourse.SubjCode)
            .ThenBy(r => r.ChildCourse.CrseNumb)
            .ThenBy(r => r.ChildCourse.SeqNumb)
            .ToListAsync(ct);

        return relationships.Select(r => ToDto(r, includeChild: true)).ToList();
    }

    public async Task<CourseRelationshipDto?> GetParentRelationshipAsync(int childCourseId, CancellationToken ct = default)
    {
        var relationship = await _context.CourseRelationships
            .AsNoTracking()
            .Include(r => r.ParentCourse)
            .FirstOrDefaultAsync(r => r.ChildCourseId == childCourseId, ct);

        return relationship == null ? null : ToDto(relationship, includeParent: true);
    }

    public async Task<List<CourseDto>> GetAvailableChildCoursesAsync(int parentCourseId, CancellationToken ct = default)
    {
        // Get the parent course to know the term
        var parentCourse = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == parentCourseId, ct);

        if (parentCourse == null)
        {
            return new List<CourseDto>();
        }

        // A child course cannot become a parent - return empty list
        // This enforces flat hierarchy (prevents multi-level: A -> B -> C)
        var isAlreadyChild = await _context.CourseRelationships
            .AsNoTracking()
            .AnyAsync(r => r.ChildCourseId == parentCourseId, ct);

        if (isAlreadyChild)
        {
            return new List<CourseDto>();
        }

        // Get IDs of courses that are already children (of any parent)
        var existingChildIds = await _context.CourseRelationships
            .AsNoTracking()
            .Select(r => r.ChildCourseId)
            .ToListAsync(ct);

        // Get IDs of courses that are already parents (have children)
        // These cannot become children to prevent multi-level hierarchies
        var existingParentIds = await _context.CourseRelationships
            .AsNoTracking()
            .Select(r => r.ParentCourseId)
            .Distinct()
            .ToListAsync(ct);

        // Get courses in the same term that:
        // - Are not the parent course itself
        // - Are not already a child of any parent
        // - Are not already a parent (to prevent multi-level hierarchies)
        var availableCourses = await _context.Courses
            .AsNoTracking()
            .Where(c => c.TermCode == parentCourse.TermCode
                && c.Id != parentCourseId
                && !existingChildIds.Contains(c.Id)
                && !existingParentIds.Contains(c.Id))
            .OrderBy(c => c.SubjCode)
            .ThenBy(c => c.CrseNumb)
            .ThenBy(c => c.SeqNumb)
            .ToListAsync(ct);

        return availableCourses.Select(ToCourseDto).ToList();
    }

    public async Task<CourseRelationshipDto> CreateRelationshipAsync(int parentCourseId, CreateCourseRelationshipRequest request, CancellationToken ct = default)
    {
        // Validate parent course exists
        var parentCourse = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == parentCourseId, ct)
            ?? throw new InvalidOperationException($"Parent course {parentCourseId} not found");

        // Prevent multi-level hierarchies: a child cannot become a parent
        var parentIsAlreadyChild = await _context.CourseRelationships
            .AsNoTracking()
            .AnyAsync(r => r.ChildCourseId == parentCourseId, ct);

        if (parentIsAlreadyChild)
        {
            throw new InvalidOperationException(
                $"Course {parentCourse.SubjCode} {parentCourse.CrseNumb}-{parentCourse.SeqNumb} cannot be a parent because it is already a child of another course");
        }

        // Validate child course exists
        var childCourse = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ChildCourseId, ct)
            ?? throw new InvalidOperationException($"Child course {request.ChildCourseId} not found");

        // Prevent multi-level hierarchies: a parent cannot become a child
        var childIsAlreadyParent = await _context.CourseRelationships
            .AsNoTracking()
            .AnyAsync(r => r.ParentCourseId == request.ChildCourseId, ct);

        if (childIsAlreadyParent)
        {
            throw new InvalidOperationException(
                $"Course {childCourse.SubjCode} {childCourse.CrseNumb}-{childCourse.SeqNumb} cannot be a child because it already has linked children");
        }

        // Validate same term
        if (parentCourse.TermCode != childCourse.TermCode)
        {
            throw new InvalidOperationException("Parent and child courses must be in the same term");
        }

        // Validate not linking to self
        if (parentCourseId == request.ChildCourseId)
        {
            throw new InvalidOperationException("A course cannot be linked to itself");
        }

        // Check if child already has a parent
        var existingParent = await _context.CourseRelationships
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ChildCourseId == request.ChildCourseId, ct);

        if (existingParent != null)
        {
            throw new InvalidOperationException($"Course {childCourse.SubjCode} {childCourse.CrseNumb} is already a child of another course");
        }

        // Check for duplicate relationship
        var existingRelationship = await _context.CourseRelationships
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ParentCourseId == parentCourseId && r.ChildCourseId == request.ChildCourseId, ct);

        if (existingRelationship != null)
        {
            throw new InvalidOperationException("This relationship already exists");
        }

        // Create the relationship
        var relationship = new CourseRelationship
        {
            ParentCourseId = parentCourseId,
            ChildCourseId = request.ChildCourseId,
            RelationshipType = request.RelationshipType
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.CourseRelationships.Add(relationship);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Unique constraint violation - child already has a parent (race condition)
            throw new InvalidOperationException(
                $"Course {childCourse.SubjCode} {childCourse.CrseNumb} is already a child of another course");
        }

        // Log audit entry
        _auditService.AddCourseChangeAudit(parentCourseId, parentCourse.TermCode, EffortAuditActions.CreateCourseRelationship,
            null,
            new
            {
                ChildCourseId = request.ChildCourseId,
                ChildCourse = $"{childCourse.SubjCode} {childCourse.CrseNumb}-{childCourse.SeqNumb}",
                RelationshipType = request.RelationshipType
            });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Created {RelationshipType} relationship: {ParentCourse} -> {ChildCourse}",
            LogSanitizer.SanitizeString(request.RelationshipType),
            $"{parentCourse.SubjCode} {parentCourse.CrseNumb}",
            $"{childCourse.SubjCode} {childCourse.CrseNumb}");

        // Return the created relationship with child course info
        relationship.ChildCourse = childCourse;
        return ToDto(relationship, includeChild: true);
    }

    public async Task<bool> DeleteRelationshipAsync(int relationshipId, CancellationToken ct = default)
    {
        var relationship = await _context.CourseRelationships
            .Include(r => r.ParentCourse)
            .Include(r => r.ChildCourse)
            .FirstOrDefaultAsync(r => r.Id == relationshipId, ct);

        if (relationship == null)
        {
            return false;
        }

        var parentCourse = relationship.ParentCourse;
        var childCourse = relationship.ChildCourse;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.CourseRelationships.Remove(relationship);

        // Log audit entry
        _auditService.AddCourseChangeAudit(relationship.ParentCourseId, parentCourse.TermCode, EffortAuditActions.DeleteCourseRelationship,
            new
            {
                ChildCourseId = relationship.ChildCourseId,
                ChildCourse = $"{childCourse.SubjCode} {childCourse.CrseNumb}-{childCourse.SeqNumb}",
                RelationshipType = relationship.RelationshipType
            },
            null);

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Deleted {RelationshipType} relationship: {ParentCourse} -> {ChildCourse}",
            LogSanitizer.SanitizeString(relationship.RelationshipType),
            $"{parentCourse.SubjCode} {parentCourse.CrseNumb}",
            $"{childCourse.SubjCode} {childCourse.CrseNumb}");

        return true;
    }

    public async Task<CourseRelationshipDto?> GetRelationshipAsync(int relationshipId, CancellationToken ct = default)
    {
        var relationship = await _context.CourseRelationships
            .AsNoTracking()
            .Include(r => r.ParentCourse)
            .Include(r => r.ChildCourse)
            .FirstOrDefaultAsync(r => r.Id == relationshipId, ct);

        return relationship == null ? null : ToDto(relationship, includeParent: true, includeChild: true);
    }

    private static CourseRelationshipDto ToDto(CourseRelationship relationship, bool includeParent = false, bool includeChild = false)
    {
        return new CourseRelationshipDto
        {
            Id = relationship.Id,
            ParentCourseId = relationship.ParentCourseId,
            ChildCourseId = relationship.ChildCourseId,
            RelationshipType = relationship.RelationshipType,
            ParentCourse = includeParent && relationship.ParentCourse != null ? ToCourseDto(relationship.ParentCourse) : null,
            ChildCourse = includeChild && relationship.ChildCourse != null ? ToCourseDto(relationship.ChildCourse) : null
        };
    }

    private static CourseDto ToCourseDto(EffortCourse course)
    {
        return new CourseDto
        {
            Id = course.Id,
            Crn = course.Crn.Trim(),
            TermCode = course.TermCode,
            SubjCode = course.SubjCode.Trim(),
            CrseNumb = course.CrseNumb.Trim(),
            SeqNumb = course.SeqNumb.Trim(),
            Enrollment = course.Enrollment,
            Units = course.Units,
            CustDept = course.CustDept.Trim()
        };
    }
}
