using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.Entities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for managing generic R-course (resident teaching) effort records.
/// Centralizes the logic for creating R-courses during harvest and on-demand creation.
/// </summary>
public class RCourseService : IRCourseService
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditService;
    private readonly ILogger<RCourseService> _logger;

    public RCourseService(
        EffortDbContext context,
        IEffortAuditService auditService,
        ILogger<RCourseService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EffortCourse> GetOrCreateGenericRCourseAsync(int termCode, CancellationToken ct = default)
    {
        var genericRCourse = await _context.Courses
            .FirstOrDefaultAsync(c => c.TermCode == termCode && c.Crn == "RESID", ct);

        if (genericRCourse != null)
        {
            return genericRCourse;
        }

        genericRCourse = new EffortCourse
        {
            TermCode = termCode,
            Crn = "RESID",
            SubjCode = "RES",
            CrseNumb = "000R",
            SeqNumb = "001",
            Units = 0,
            Enrollment = 0,
            CustDept = string.Empty
        };
        _context.Courses.Add(genericRCourse);
        await _context.SaveChangesAsync(ct);

        _auditService.AddCourseChangeAudit(
            genericRCourse.Id,
            termCode,
            EffortAuditActions.CreateCourse,
            null,
            new
            {
                genericRCourse.Crn,
                genericRCourse.SubjCode,
                genericRCourse.CrseNumb,
                genericRCourse.SeqNumb
            });

        _logger.LogInformation("Created generic R-course for term {TermCode}", termCode);

        return genericRCourse;
    }

    /// <inheritdoc />
    public async Task CreateRCourseEffortRecordAsync(
        int personId,
        int termCode,
        int modifiedBy,
        RCourseCreationContext context,
        CancellationToken ct = default)
    {
        var genericRCourse = await GetOrCreateGenericRCourseAsync(termCode, ct);

        // Check if effort record already exists (idempotent)
        var existingRecord = await _context.Records
            .AnyAsync(r => r.PersonId == personId
                        && r.CourseId == genericRCourse.Id
                        && r.TermCode == termCode, ct);

        if (existingRecord)
        {
            return;
        }

        // Get default effort type for R-courses (first active type with AllowedOnRCourses=true, ordered by Id)
        var defaultEffortType = await _context.EffortTypes
            .Where(t => t.AllowedOnRCourses && t.IsActive)
            .OrderBy(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (defaultEffortType == null)
        {
            _logger.LogWarning(
                "Cannot create R-course for PersonId {PersonId} in term {TermCode}: " +
                "No active effort type with AllowedOnRCourses=true exists",
                personId, termCode);
            return;
        }

        var effortRecord = new EffortRecord
        {
            PersonId = personId,
            CourseId = genericRCourse.Id,
            TermCode = termCode,
            Hours = defaultEffortType.UsesWeeks ? null : 0,
            Weeks = defaultEffortType.UsesWeeks ? 1 : null,
            EffortTypeId = defaultEffortType.Id,
            RoleId = EffortConstants.InstructorRoleId,
            Crn = "RESID",
            ModifiedDate = DateTime.Now,
            ModifiedBy = modifiedBy
        };
        _context.Records.Add(effortRecord);
        await _context.SaveChangesAsync(ct);

        // Create audit entry with context-specific details
        var details = context == RCourseCreationContext.Harvest
            ? "Generic R-course effort record auto-created during harvest"
            : "Generic R-course effort record auto-created when first non-R-course added";

        var auditEntry = new Audit
        {
            TableName = EffortAuditTables.Records,
            RecordId = effortRecord.Id,
            TermCode = termCode,
            Action = EffortAuditActions.RCourseAutoCreated,
            ChangedBy = modifiedBy,
            ChangedDate = DateTime.Now,
            Changes = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["PersonId"] = personId,
                ["Course"] = "RES 000R",
                ["Type"] = defaultEffortType.Id,
                ["Details"] = details
            })
        };
        _context.Audits.Add(auditEntry);
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Created R-course effort record for PersonId {PersonId} in term {TermCode} (context: {Context})",
            personId, termCode, context);
    }
}
