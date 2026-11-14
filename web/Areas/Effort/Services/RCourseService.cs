using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
    public async Task CreateForEligibleInstructorsAsync(int termCode, int modifiedBy, RCourseCreationContext context, CancellationToken ct = default)
    {
        var eligibleInstructors = await _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode && p.EffortVerified == null)
            .Select(p => p.PersonId)
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} eligible instructors for R-course generation in term {TermCode}",
            eligibleInstructors.Count, termCode);

        await CreateRecordsAsync(eligibleInstructors, termCode, modifiedBy, context, ct);
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
    public Task CreateRCourseEffortRecordAsync(
        int personId,
        int termCode,
        int modifiedBy,
        RCourseCreationContext context,
        CancellationToken ct = default) =>
        CreateRecordsAsync([personId], termCode, modifiedBy, context, ct);

    private async Task CreateRecordsAsync(
        List<int> personIds,
        int termCode,
        int modifiedBy,
        RCourseCreationContext context,
        CancellationToken ct)
    {
        if (personIds.Count == 0)
        {
            return;
        }

        var genericRCourse = await GetOrCreateGenericRCourseAsync(termCode, ct);

        // Skip instructors who already have the record (idempotent)
        var existingPersonIds = await _context.Records
            .AsNoTracking()
            .Where(r => r.CourseId == genericRCourse.Id
                     && r.TermCode == termCode
                     && EF.Parameter(personIds).Contains(r.PersonId))
            .Select(r => r.PersonId)
            .ToListAsync(ct);
        var missingPersonIds = personIds.Except(existingPersonIds).ToList();

        if (missingPersonIds.Count == 0)
        {
            return;
        }

        // Prefer VAR; otherwise fall back to the first active R-course type by Id
        var defaultEffortType = await _context.EffortTypes
            .Where(t => t.AllowedOnRCourses && t.IsActive)
            .OrderBy(t => t.Id != EffortConstants.VariableEffortType)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (defaultEffortType == null)
        {
            _logger.LogWarning(
                "Cannot create R-course for {Count} instructors in term {TermCode}: " +
                "No active effort type with AllowedOnRCourses=true exists",
                missingPersonIds.Count, termCode);
            return;
        }

        var effortRecords = missingPersonIds.Select(personId => new EffortRecord
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
        }).ToList();
        _context.Records.AddRange(effortRecords);
        await _context.SaveChangesAsync(ct);

        // Create audit entries with context-specific details
        var details = context switch
        {
            RCourseCreationContext.Harvest => "Generic R-course effort record auto-created during harvest",
            RCourseCreationContext.Reopen => "Generic R-course effort record auto-created when term reopened",
            RCourseCreationContext.InstructorAdded => "Generic R-course effort record auto-created when instructor added",
            _ => "Generic R-course effort record auto-created when first non-R-course added"
        };

        _context.Audits.AddRange(effortRecords.Select(effortRecord => new Audit
        {
            TableName = EffortAuditTables.Records,
            RecordId = effortRecord.Id,
            TermCode = termCode,
            Action = EffortAuditActions.RCourseAutoCreated,
            ChangedBy = modifiedBy,
            ChangedDate = DateTime.Now,
            Changes = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["PersonId"] = effortRecord.PersonId,
                ["Course"] = "RES 000R",
                ["Type"] = defaultEffortType.Id,
                ["Details"] = details
            })
        }));
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Created {Count} R-course effort records in term {TermCode} (context: {Context})",
            effortRecords.Count, termCode, context);
    }
}
