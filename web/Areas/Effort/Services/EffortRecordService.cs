using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Exceptions;
using Viper.Areas.Effort.Models.DTOs;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for effort record CRUD operations.
/// Implements business rules from legacy ColdFusion system.
/// </summary>
public class EffortRecordService : IEffortRecordService
{
    private readonly EffortDbContext _context;
    private readonly RAPSContext _rapsContext;
    private readonly IEffortAuditService _auditService;
    private readonly IInstructorService _instructorService;
    private readonly ICourseClassificationService _courseClassificationService;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<EffortRecordService> _logger;

    public EffortRecordService(
        EffortDbContext context,
        RAPSContext rapsContext,
        IEffortAuditService auditService,
        IInstructorService instructorService,
        ICourseClassificationService courseClassificationService,
        IUserHelper userHelper,
        ILogger<EffortRecordService> logger)
    {
        _context = context;
        _rapsContext = rapsContext;
        _auditService = auditService;
        _instructorService = instructorService;
        _courseClassificationService = courseClassificationService;
        _userHelper = userHelper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<InstructorEffortRecordDto?> GetEffortRecordAsync(int recordId, CancellationToken ct = default)
    {
        var record = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Include(r => r.RoleNavigation)
            .Where(r => r.Id == recordId)
            .FirstOrDefaultAsync(ct);

        if (record == null)
        {
            return null;
        }

        return MapToDto(record);
    }

    /// <inheritdoc />
    public async Task<(InstructorEffortRecordDto Record, string? Warning)> CreateEffortRecordAsync(
        CreateEffortRecordRequest request, CancellationToken ct = default)
    {
        // Validate effort value is non-negative
        if (request.EffortValue < 0)
        {
            throw new InvalidOperationException("Effort value must be non-negative.");
        }

        // Get or create instructor for this term (matches legacy EffortAction.cfm behavior)
        var person = await _context.Persons
            .FirstOrDefaultAsync(p => p.PersonId == request.PersonId && p.TermCode == request.TermCode, ct);

        if (person == null)
        {
            // Auto-create instructor if they don't exist (legacy parity)
            _logger.LogInformation(
                "Auto-creating instructor {PersonId} for term {TermCode} during effort record creation",
                request.PersonId, request.TermCode);

            try
            {
                await _instructorService.CreateInstructorAsync(
                    new CreateInstructorRequest { PersonId = request.PersonId, TermCode = request.TermCode }, ct);
            }
            catch (InstructorAlreadyExistsException ex)
            {
                // Race condition: another request created the instructor concurrently
                _logger.LogDebug(ex,
                    "Instructor {PersonId} for term {TermCode} was created by concurrent request",
                    request.PersonId, request.TermCode);
            }
            catch (InvalidOperationException ex)
            {
                // CreateInstructorAsync failed (e.g., VIPER person not found)
                throw new InvalidOperationException(
                    $"Failed to create instructor for PersonId {request.PersonId}: {ex.Message}", ex);
            }

            // Re-fetch the newly created person
            person = await _context.Persons
                .FirstOrDefaultAsync(p => p.PersonId == request.PersonId && p.TermCode == request.TermCode, ct);

            if (person == null)
            {
                throw new InvalidOperationException($"Person with PersonId {request.PersonId} not found after instructor creation");
            }
        }

        // Validate course exists and is not a child course
        var course = await _context.Courses
            .Include(c => c.ChildRelationships)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.TermCode == request.TermCode, ct);

        if (course == null)
        {
            throw new InvalidOperationException($"Course {request.CourseId} not found for term {request.TermCode}");
        }

        // Check if this course is a child in any relationship (has a parent)
        if (course.ChildRelationships.Any())
        {
            throw new InvalidOperationException("Cannot add effort to a child course. Add effort to the parent course instead.");
        }

        // Validate effort type is active
        var effortType = await _context.EffortTypes
            .FirstOrDefaultAsync(et => et.Id == request.EffortTypeId && et.IsActive, ct);

        if (effortType == null)
        {
            throw new InvalidOperationException($"Effort type '{request.EffortTypeId}' not found or inactive");
        }

        // Validate effort type is allowed for this course category
        ValidateEffortTypeForCourse(effortType, course);

        // Validate role is active
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.IsActive, ct);

        if (role == null)
        {
            throw new InvalidOperationException($"Role {request.RoleId} not found or inactive");
        }

        // Check for duplicate: (CourseId, PersonId, EffortTypeId) must be unique
        var existingRecord = await _context.Records
            .FirstOrDefaultAsync(r =>
                r.CourseId == request.CourseId &&
                r.PersonId == request.PersonId &&
                r.EffortTypeId == request.EffortTypeId, ct);

        if (existingRecord != null)
        {
            throw new InvalidOperationException(
                $"An effort record for {course.SubjCode.Trim()} {course.CrseNumb.Trim()} with type {request.EffortTypeId} already exists for this instructor");
        }

        // Check role consistency - coerce if needed
        string? warning = null;
        int? coercedFromRoleId = null;
        string? coercedFromRoleDescription = null;
        var existingRecordsForCourse = await _context.Records
            .Include(r => r.RoleNavigation)
            .Where(r => r.CourseId == request.CourseId && r.PersonId == request.PersonId && r.RoleId != request.RoleId)
            .ToListAsync(ct);

        if (existingRecordsForCourse.Count > 0)
        {
            // Capture the old role before coercion for audit
            coercedFromRoleId = existingRecordsForCourse[0].RoleId;
            coercedFromRoleDescription = existingRecordsForCourse[0].RoleNavigation?.Description;

            // Coerce all records for same course+person to new role
            foreach (var existingRec in existingRecordsForCourse)
            {
                existingRec.RoleId = request.RoleId;
                existingRec.ModifiedDate = DateTime.Now;
                existingRec.ModifiedBy = GetCurrentPersonId();
            }
            warning = $"Role was updated to \"{role.Description}\" for all effort records on this course.";
        }

        // Determine hours vs weeks
        var useWeeks = UsesWeeks(request.EffortTypeId, request.TermCode);

        var record = new EffortRecord
        {
            CourseId = request.CourseId,
            PersonId = request.PersonId,
            TermCode = request.TermCode,
            EffortTypeId = request.EffortTypeId,
            RoleId = request.RoleId,
            Hours = useWeeks ? null : request.EffortValue,
            Weeks = useWeeks ? request.EffortValue : null,
            Crn = course.Crn,
            ModifiedDate = DateTime.Now,
            ModifiedBy = GetCurrentPersonId()
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        _context.Records.Add(record);

        // Handle verification - only clear if NOT a self-edit
        HandleVerificationOnEdit(person, GetCurrentPersonId(), "create");

        await _context.SaveChangesAsync(ct);

        // Log audit - include role coercion details if it occurred
        await _auditService.LogRecordChangeAsync(record.Id, request.TermCode, EffortAuditActions.CreateEffort,
            null,
            new
            {
                Course = $"{course.SubjCode.Trim()} {course.CrseNumb.Trim()}",
                EffortType = request.EffortTypeId,
                Role = role.Description,
                Hours = record.Hours,
                Weeks = record.Weeks,
                RolesCoerced = coercedFromRoleId != null
                    ? $"{existingRecordsForCourse.Count} record(s) changed from '{coercedFromRoleDescription}' to '{role.Description}'"
                    : null
            }, ct);

        await transaction.CommitAsync(ct);

        // Reload with navigation properties
        var createdRecord = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Include(r => r.RoleNavigation)
            .FirstAsync(r => r.Id == record.Id, ct);

        return (MapToDto(createdRecord), warning);
    }

    /// <inheritdoc />
    public async Task<(InstructorEffortRecordDto? Record, string? Warning)> UpdateEffortRecordAsync(
        int recordId, UpdateEffortRecordRequest request, CancellationToken ct = default)
    {
        // Validate effort value is non-negative
        if (request.EffortValue < 0)
        {
            throw new InvalidOperationException("Effort value must be non-negative.");
        }

        var record = await _context.Records
            .Include(r => r.Course)
            .Include(r => r.Person)
            .Include(r => r.RoleNavigation)
            .FirstOrDefaultAsync(r => r.Id == recordId, ct);

        if (record == null)
        {
            return (null, null);
        }

        // Validate effort type is active
        var effortType = await _context.EffortTypes
            .FirstOrDefaultAsync(et => et.Id == request.EffortTypeId && et.IsActive, ct);

        if (effortType == null)
        {
            throw new InvalidOperationException($"Effort type '{request.EffortTypeId}' not found or inactive");
        }

        // Validate effort type is allowed for this course category
        ValidateEffortTypeForCourse(effortType, record.Course);

        // Validate role is active
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.IsActive, ct);

        if (role == null)
        {
            throw new InvalidOperationException($"Role {request.RoleId} not found or inactive");
        }

        // If effort type is changing, check for duplicate
        if (record.EffortTypeId != request.EffortTypeId)
        {
            var existingRecord = await _context.Records
                .FirstOrDefaultAsync(r =>
                    r.Id != recordId &&
                    r.CourseId == record.CourseId &&
                    r.PersonId == record.PersonId &&
                    r.EffortTypeId == request.EffortTypeId, ct);

            if (existingRecord != null)
            {
                throw new InvalidOperationException(
                    $"An effort record for {record.Course.SubjCode.Trim()} {record.Course.CrseNumb.Trim()} with type {request.EffortTypeId} already exists for this instructor");
            }
        }

        // Capture old values for audit
        var oldValues = new
        {
            EffortType = record.EffortTypeId,
            Role = record.RoleNavigation?.Description,
            record.Hours,
            record.Weeks
        };

        // Check role consistency - coerce if needed
        string? warning = null;
        string? coercedFromRoleDescription = null;
        var existingRecordsForCourse = await _context.Records
            .Include(r => r.RoleNavigation)
            .Where(r => r.Id != recordId && r.CourseId == record.CourseId && r.PersonId == record.PersonId && r.RoleId != request.RoleId)
            .ToListAsync(ct);

        if (existingRecordsForCourse.Count > 0)
        {
            // Capture the old role before coercion for audit
            coercedFromRoleDescription = existingRecordsForCourse[0].RoleNavigation?.Description;

            // Coerce all records for same course+person to new role
            foreach (var existingRec in existingRecordsForCourse)
            {
                existingRec.RoleId = request.RoleId;
                existingRec.ModifiedDate = DateTime.Now;
                existingRec.ModifiedBy = GetCurrentPersonId();
            }
            warning = $"Role was updated to \"{role.Description}\" for all effort records on this course.";
        }

        // Determine hours vs weeks
        var useWeeks = UsesWeeks(request.EffortTypeId, record.TermCode);

        // Update record
        record.EffortTypeId = request.EffortTypeId;
        record.RoleId = request.RoleId;
        record.Hours = useWeeks ? null : request.EffortValue;
        record.Weeks = useWeeks ? request.EffortValue : null;
        record.ModifiedDate = DateTime.Now;
        record.ModifiedBy = GetCurrentPersonId();

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        // Handle verification - only clear if NOT a self-edit
        HandleVerificationOnEdit(record.Person, GetCurrentPersonId(), "update");

        await _context.SaveChangesAsync(ct);

        // Log audit - include role coercion details if it occurred
        await _auditService.LogRecordChangeAsync(recordId, record.TermCode, EffortAuditActions.UpdateEffort,
            oldValues,
            new
            {
                EffortType = request.EffortTypeId,
                Role = role.Description,
                Hours = record.Hours,
                Weeks = record.Weeks,
                RolesCoerced = coercedFromRoleDescription != null
                    ? $"{existingRecordsForCourse.Count} record(s) changed from '{coercedFromRoleDescription}' to '{role.Description}'"
                    : null
            }, ct);

        await transaction.CommitAsync(ct);

        // Reload with navigation properties
        var updatedRecord = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Include(r => r.RoleNavigation)
            .FirstAsync(r => r.Id == recordId, ct);

        return (MapToDto(updatedRecord), warning);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteEffortRecordAsync(int recordId, CancellationToken ct = default)
    {
        var record = await _context.Records
            .Include(r => r.Course)
            .Include(r => r.Person)
            .Include(r => r.RoleNavigation)
            .FirstOrDefaultAsync(r => r.Id == recordId, ct);

        if (record == null)
        {
            return false;
        }

        // Capture values for audit
        var oldValues = new
        {
            Course = $"{record.Course.SubjCode.Trim()} {record.Course.CrseNumb.Trim()}",
            EffortType = record.EffortTypeId,
            Role = record.RoleNavigation?.Description,
            record.Hours,
            record.Weeks
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        // Handle verification - only clear if NOT a self-edit
        HandleVerificationOnEdit(record.Person, GetCurrentPersonId(), "delete");

        _context.Records.Remove(record);

        await _context.SaveChangesAsync(ct);

        // Log audit
        await _auditService.LogRecordChangeAsync(recordId, record.TermCode, EffortAuditActions.DeleteEffort,
            oldValues, null, ct);

        await transaction.CommitAsync(ct);

        return true;
    }

    /// <inheritdoc />
    public async Task<AvailableCoursesDto> GetAvailableCoursesAsync(int personId, int termCode, CancellationToken ct = default)
    {
        // Get courses that already have effort records for this person
        var existingCourseIds = await _context.Records
            .AsNoTracking()
            .Where(r => r.PersonId == personId && r.TermCode == termCode)
            .Select(r => r.CourseId)
            .Distinct()
            .ToListAsync(ct);

        // Get child course IDs for the term - exclude these from "All Courses"
        // but always include them in "Existing Courses" if instructor has effort on them
        var childCourseIds = await _context.CourseRelationships
            .AsNoTracking()
            .Where(cr => _context.Courses.Any(c => c.Id == cr.ChildCourseId && c.TermCode == termCode))
            .Select(cr => cr.ChildCourseId)
            .Distinct()
            .ToListAsync(ct);

        // Get all courses for the term (needed to map existing courses)
        var allCoursesForTerm = await _context.Courses
            .AsNoTracking()
            .Where(c => c.TermCode == termCode)
            .OrderBy(c => c.SubjCode)
            .ThenBy(c => c.CrseNumb)
            .ThenBy(c => c.SeqNumb)
            .ToListAsync(ct);

        // Existing courses: courses instructor already has effort on
        // Always include these even if they are child courses
        var existingCourses = allCoursesForTerm
            .Where(c => existingCourseIds.Contains(c.Id))
            .Select(MapToCourseOption)
            .ToList();

        // All courses: exclude child courses (effort should go on parent)
        var allCourseOptions = allCoursesForTerm
            .Where(c => !childCourseIds.Contains(c.Id))
            .Select(MapToCourseOption)
            .ToList();

        return new AvailableCoursesDto
        {
            ExistingCourses = existingCourses,
            AllCourses = allCourseOptions
        };
    }

    /// <inheritdoc />
    public async Task<List<EffortTypeOptionDto>> GetEffortTypeOptionsAsync(CancellationToken ct = default)
    {
        return await _context.EffortTypes
            .AsNoTracking()
            .Where(et => et.IsActive)
            .OrderBy(et => et.Id)
            .Select(et => new EffortTypeOptionDto
            {
                Id = et.Id,
                Description = et.Description,
                UsesWeeks = et.UsesWeeks,
                AllowedOnDvm = et.AllowedOnDvm,
                AllowedOn199299 = et.AllowedOn199299,
                AllowedOnRCourses = et.AllowedOnRCourses
            })
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<RoleOptionDto>> GetRoleOptionsAsync(CancellationToken ct = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder ?? r.Id)
            .Select(r => new RoleOptionDto
            {
                Id = r.Id,
                Description = r.Description
            })
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanEditTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermCode == termCode, ct);

        if (term == null)
        {
            return false;
        }

        // If term is open, editing is allowed
        if (term.Status == "Opened")
        {
            return true;
        }

        // Otherwise check if user has EditWhenClosed permission
        var user = _userHelper.GetCurrentUser();
        if (user == null)
        {
            return false;
        }

        return _userHelper.HasPermission(_rapsContext, user, EffortPermissions.EditWhenClosed);
    }

    /// <inheritdoc />
    public bool UsesWeeks(string effortTypeId, int termCode)
    {
        // CLI uses weeks only when termCode >= ClinicalAsWeeksStartTermCode
        if (string.Equals(effortTypeId, EffortConstants.ClinicalEffortType, StringComparison.OrdinalIgnoreCase))
        {
            return termCode >= EffortConstants.ClinicalAsWeeksStartTermCode;
        }

        // All other effort types use hours
        return false;
    }

    private int GetCurrentPersonId()
    {
        var user = _userHelper.GetCurrentUser();
        return user?.AaudUserId ?? 0;
    }

    /// <summary>
    /// Clears verification status if the edit is NOT a self-edit.
    /// EffortPerson is term-scoped, so we need PersonId + TermCode to find the correct record.
    /// </summary>
    private void HandleVerificationOnEdit(EffortPerson person, int editedByPersonId, string action)
    {
        var isSelfEdit = editedByPersonId == person.PersonId;

        // Only clear verification if NOT a self-edit
        if (!isSelfEdit && person.EffortVerified != null)
        {
            _logger.LogInformation(
                "Clearing EffortVerified for PersonId {PersonId} in term {TermCode} due to {Action} by PersonId {EditedBy}",
                person.PersonId, person.TermCode, action, editedByPersonId);
            person.EffortVerified = null;
        }
    }

    private InstructorEffortRecordDto MapToDto(EffortRecord record)
    {
        var classification = _courseClassificationService.Classify(record.Course);
        return new InstructorEffortRecordDto
        {
            Id = record.Id,
            CourseId = record.CourseId,
            PersonId = record.PersonId,
            TermCode = record.TermCode,
            EffortType = record.EffortTypeId,
            Role = record.RoleId,
            RoleDescription = record.RoleNavigation?.Description ?? "",
            Hours = record.Hours,
            Weeks = record.Weeks,
            Crn = record.Crn,
            ModifiedDate = record.ModifiedDate,
            Course = new CourseDto
            {
                Id = record.Course.Id,
                Crn = record.Course.Crn,
                TermCode = record.Course.TermCode,
                SubjCode = record.Course.SubjCode,
                CrseNumb = record.Course.CrseNumb,
                SeqNumb = record.Course.SeqNumb,
                Enrollment = record.Course.Enrollment,
                Units = record.Course.Units,
                CustDept = record.Course.CustDept
            }.WithClassification(classification)
        };
    }

    private CourseOptionDto MapToCourseOption(EffortCourse course)
    {
        var subjCode = course.SubjCode.Trim();
        var crseNumb = course.CrseNumb.Trim();
        var seqNumb = course.SeqNumb.Trim();
        var classification = _courseClassificationService.Classify(course);

        return new CourseOptionDto
        {
            Id = course.Id,
            SubjCode = subjCode,
            CrseNumb = crseNumb,
            SeqNumb = seqNumb,
            Units = course.Units,
            Label = $"{subjCode} {crseNumb}-{seqNumb} ({course.Units} units)",
            Crn = course.Crn.Trim()
        }.WithClassification(classification);
    }

    private void ValidateEffortTypeForCourse(EffortType effortType, EffortCourse course)
    {
        var classification = _courseClassificationService.Classify(course);

        // Use AND logic - check ALL applicable classifications
        var errors = new List<string>();

        if (classification.IsDvmCourse && !effortType.AllowedOnDvm)
        {
            errors.Add("DVM courses");
        }

        if (classification.Is199299Course && !effortType.AllowedOn199299)
        {
            errors.Add("199/299 courses");
        }

        if (classification.IsRCourse && !effortType.AllowedOnRCourses)
        {
            errors.Add("R courses");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Effort type '{effortType.Description}' is not allowed on {string.Join(" or ", errors)}");
        }
    }
}
