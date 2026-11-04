using System.Data;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Curriculum.Services;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Helpers;
using Viper.Areas.Effort.Models;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for term-related operations.
/// </summary>
public class TermService : ITermService
{
    private const string CloseCleanupReason = "TermClose";

    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly IEffortAuditService _auditService;
    private readonly ICourseClassificationService _classificationService;

    public TermService(EffortDbContext context, VIPERContext viperContext, IEffortAuditService auditService, ICourseClassificationService classificationService)
    {
        _context = context;
        _viperContext = viperContext;
        _auditService = auditService;
        _classificationService = classificationService;
    }

    public async Task<List<TermDto>> GetTermsAsync(CancellationToken ct = default)
    {
        var terms = await _context.Terms
            .AsNoTracking()
            .OrderByDescending(t => t.TermCode)
            .ToListAsync(ct);

        var termCodes = terms.Select(t => t.TermCode).ToList();

        // Get term codes that have related data (cannot be deleted)
        var termsWithPersons = await _context.Persons
            .Where(p => EF.Parameter(termCodes).Contains(p.TermCode))
            .Select(p => p.TermCode)
            .Distinct()
            .ToListAsync(ct);
        var termsWithCourses = await _context.Courses
            .Where(c => EF.Parameter(termCodes).Contains(c.TermCode))
            .Select(c => c.TermCode)
            .Distinct()
            .ToListAsync(ct);
        var termsWithRecords = await _context.Records
            .Where(r => EF.Parameter(termCodes).Contains(r.TermCode))
            .Select(r => r.TermCode)
            .Distinct()
            .ToListAsync(ct);

        var termsWithData = termsWithPersons
            .Union(termsWithCourses)
            .Union(termsWithRecords)
            .ToHashSet();

        var termEndDates = await _viperContext.Terms
            .AsNoTracking()
            .Where(t => EF.Parameter(termCodes).Contains(t.TermCode))
            .ToDictionaryAsync(t => t.TermCode, t => t.EndDate, ct);

        return terms.Select(t =>
        {
            var dto = EffortMapper.ToTermDto(t);
            dto.TermName = GetTermName(t.TermCode);
            dto.TermEndDate = termEndDates.TryGetValue(t.TermCode, out var endDate) ? endDate : null;
            dto.CanDelete = !termsWithData.Contains(t.TermCode);
            // Use term status and term code for clinical import eligibility
            dto.CanImportClinical = TermValidationHelper.CanImportClinical(dto.Status, t.TermCode);
            return dto;
        }).ToList();
    }

    public async Task<TermDto?> GetTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermCode == termCode, ct);

        if (term == null) return null;
        var dto = await MapTermToDtoAsync(term, ct);
        // Use term status and term code for clinical import eligibility
        dto.CanImportClinical = TermValidationHelper.CanImportClinical(dto.Status, termCode);
        return dto;
    }

    public async Task<TermDto?> GetCurrentTermAsync(CancellationToken ct = default)
    {
        // Open = OpenedDate IS NOT NULL AND ClosedDate IS NULL (matches legacy logic)
        var term = await _context.Terms
            .AsNoTracking()
            .Where(t => t.OpenedDate != null && t.ClosedDate == null)
            .OrderByDescending(t => t.TermCode)
            .FirstOrDefaultAsync(ct);

        if (term == null) return null;
        return await MapTermToDtoAsync(term, ct);
    }

    public string GetTermName(int termCode)
    {
        var termName = TermCodeService.GetTermCodeDescription(termCode);
        return termName.StartsWith("Unknown Term") ? $"Term {termCode}" : termName;
    }

    public async Task<string?> GetTermTypeAsync(int termCode, CancellationToken ct = default)
    {
        var viperTerm = await _viperContext.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        return viperTerm?.TermType;
    }

    /// <summary>
    /// Validates that an expected close date falls within a reasonable range
    /// relative to the term's end date: must be after term end and within 1 year of it.
    /// </summary>
    private async Task ValidateExpectedCloseDateAsync(int termCode, DateTime expectedCloseDate, CancellationToken ct)
    {
        var termEndDate = await GetTermEndDateAsync(termCode, ct);
        if (termEndDate == null) return; // Can't validate against unknown term

        if (expectedCloseDate.Date <= termEndDate.Value.Date)
        {
            throw new InvalidOperationException(
                $"Expected close date must be after the term end date ({termEndDate.Value:M/d/yyyy})");
        }

        if (expectedCloseDate.Date > termEndDate.Value.Date.AddYears(1))
        {
            throw new InvalidOperationException(
                $"Expected close date cannot be more than 1 year after the term end date ({termEndDate.Value:M/d/yyyy})");
        }
    }

    private async Task<DateTime?> GetTermEndDateAsync(int termCode, CancellationToken ct)
    {
        return await _viperContext.Terms
            .AsNoTracking()
            .Where(t => t.TermCode == termCode)
            .Select(t => (DateTime?)t.EndDate)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Maps an EffortTerm entity to a TermDto, enriching it with the term name and end date.
    /// </summary>
    private async Task<TermDto> MapTermToDtoAsync(EffortTerm term, CancellationToken ct)
    {
        var dto = EffortMapper.ToTermDto(term);
        dto.TermName = GetTermName(term.TermCode);
        dto.TermEndDate = await GetTermEndDateAsync(term.TermCode, ct);
        return dto;
    }

    // Term Management Operations

    public async Task<TermDto> CreateTermAsync(int termCode, DateTime? expectedCloseDate = null, CancellationToken ct = default)
    {
        var existingTerm = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (existingTerm != null)
        {
            throw new InvalidOperationException($"Term {termCode} already exists");
        }

        if (expectedCloseDate.HasValue)
        {
            await ValidateExpectedCloseDateAsync(termCode, expectedCloseDate.Value, ct);
        }

        // New term starts with no dates set (Status will be computed as "Created")
        var term = new EffortTerm
        {
            TermCode = termCode,
            ExpectedCloseDate = expectedCloseDate
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.Terms.Add(term);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.CreateTerm,
            null,
            new { term.Status, term.ExpectedCloseDate });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await MapTermToDtoAsync(term, ct);
    }

    public async Task<TermDto?> UpdateExpectedCloseDateAsync(int termCode, DateTime? expectedCloseDate, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        if (term.ClosedDate.HasValue)
        {
            throw new InvalidOperationException("Cannot update expected close date for a closed term");
        }

        if (expectedCloseDate.HasValue)
        {
            await ValidateExpectedCloseDateAsync(termCode, expectedCloseDate.Value, ct);
        }

        var oldDate = term.ExpectedCloseDate;

        // Skip save+audit if nothing changed
        if (oldDate != expectedCloseDate)
        {
            term.ExpectedCloseDate = expectedCloseDate;

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            _auditService.AddTermChangeAudit(termCode, EffortAuditActions.UpdateTerm,
                new { ExpectedCloseDate = oldDate },
                new { ExpectedCloseDate = expectedCloseDate });
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        return await MapTermToDtoAsync(term, ct);
    }

    public async Task<bool> DeleteTermAsync(int termCode, CancellationToken ct = default)
    {
        if (!await CanDeleteTermAsync(termCode, ct))
        {
            return false;
        }

        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return false;
        }

        var oldStatus = term.Status;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.Terms.Remove(term);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.DeleteTerm,
            new { Status = oldStatus },
            null);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }

    public async Task<TermDto?> OpenTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        // Can only open if not yet opened and not closed
        if (term.OpenedDate.HasValue || term.ClosedDate.HasValue)
        {
            throw new InvalidOperationException("Term must be in Created or Harvested status to open");
        }

        var oldStatus = term.Status;
        term.OpenedDate = DateTime.Now;
        term.ClosedDate = null;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.OpenTerm,
            new { Status = oldStatus },
            new { term.Status, term.OpenedDate });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await MapTermToDtoAsync(term, ct);
    }

    public async Task<(bool Success, string? ErrorMessage)> CloseTermAsync(int termCode, CancellationToken ct = default)
    {
        // Serializable so an edit saved during the close cannot be deleted as zero effort
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return (false, "Term not found");
        }

        // Can only close if currently open (OpenedDate set, ClosedDate not set)
        if (!term.OpenedDate.HasValue || term.ClosedDate.HasValue)
        {
            return (false, "Term must be opened before it can be closed");
        }

        var cleanup = await BuildCloseCleanupAsync(termCode, QueryTrackingBehavior.TrackAll, ct);
        if (cleanup.BlockingCourses.Count > 0)
        {
            var courseCodes = string.Join(", ", cleanup.BlockingCourses.Select(FormatCourseCode));
            return (false, $"Cannot close term: {cleanup.BlockingCourses.Count} course(s) have zero enrollment and effort on the course or its parent: {courseCodes}");
        }

        var oldStatus = term.Status;
        term.ClosedDate = DateTime.Now;

        _context.Records.RemoveRange(cleanup.Records);
        foreach (var record in cleanup.Records)
        {
            _auditService.AddRecordChangeAudit(record.Id, termCode, EffortAuditActions.DeleteEffort,
                new
                {
                    Reason = CloseCleanupReason,
                    record.PersonId,
                    Course = FormatCourseCode(record.Course),
                    EffortType = record.EffortTypeId,
                    record.Hours,
                    record.Weeks,
                    record.Notes
                },
                null);
        }

        _context.CourseRelationships.RemoveRange(cleanup.Relationships);
        _context.Courses.RemoveRange(cleanup.Courses);
        foreach (var course in cleanup.Courses)
        {
            _auditService.AddCourseChangeAudit(course.Id, termCode, EffortAuditActions.DeleteCourse,
                new
                {
                    Reason = CloseCleanupReason,
                    course.Crn,
                    course.SubjCode,
                    course.CrseNumb,
                    course.SeqNumb,
                    course.Enrollment,
                    course.Units,
                    course.CustDept
                },
                null);
        }

        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.CloseTerm,
            new { Status = oldStatus },
            new
            {
                term.Status,
                term.ClosedDate,
                ZeroEffortRecordsRemoved = cleanup.Records.Count,
                ZeroEnrollmentCoursesRemoved = cleanup.Courses.Count
            });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return (true, null);
    }

    public async Task<TermDto?> ReopenTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        // Can only reopen if currently closed (ClosedDate is set)
        if (!term.ClosedDate.HasValue)
        {
            throw new InvalidOperationException("Only closed terms can be reopened");
        }

        var oldStatus = term.Status;
        term.ClosedDate = null;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.ReopenTerm,
            new { Status = oldStatus },
            new { term.Status });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await MapTermToDtoAsync(term, ct);
    }

    public async Task<TermDto?> UnopenTermAsync(int termCode, CancellationToken ct = default)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return null;
        }

        // Can only unopen if currently open (OpenedDate set, ClosedDate not set)
        if (!term.OpenedDate.HasValue || term.ClosedDate.HasValue)
        {
            throw new InvalidOperationException("Only opened terms can be reverted to unopened");
        }

        var oldStatus = term.Status;
        // Clear dates to revert - Status will compute based on remaining dates
        term.OpenedDate = null;
        term.ClosedDate = null;

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _auditService.AddTermChangeAudit(termCode, EffortAuditActions.UnopenTerm,
            new { Status = oldStatus },
            new { term.Status });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return await MapTermToDtoAsync(term, ct);
    }

    public async Task<bool> CanDeleteTermAsync(int termCode, CancellationToken ct = default)
    {
        var hasPersons = await _context.Persons.AnyAsync(p => p.TermCode == termCode, ct);
        if (hasPersons) return false;

        var hasCourses = await _context.Courses.AnyAsync(c => c.TermCode == termCode, ct);
        if (hasCourses) return false;

        var hasRecords = await _context.Records.AnyAsync(r => r.TermCode == termCode, ct);
        if (hasRecords) return false;

        return true;
    }

    public async Task<(bool CanClose, int BlockingCourseCount)> CanCloseTermAsync(int termCode, CancellationToken ct = default)
    {
        var cleanup = await BuildCloseCleanupAsync(termCode, QueryTrackingBehavior.NoTracking, ct);

        return (cleanup.BlockingCourses.Count == 0, cleanup.BlockingCourses.Count);
    }

    /// <summary>
    /// Works out what closing a term would remove and which courses block it, without changing anything.
    /// </summary>
    private async Task<CloseCleanup> BuildCloseCleanupAsync(int termCode, QueryTrackingBehavior tracking, CancellationToken ct)
    {
        // Both columns must be empty: a weeks-based record has Hours = null, so checking Hours alone would delete real effort
        var zeroEffortRecords = await _context.Records
            .AsTracking(tracking)
            .Include(r => r.Course)
            .Where(r => r.TermCode == termCode
                     && (r.Hours ?? 0) == 0
                     && (r.Weeks ?? 0) == 0
                     && r.Person.EffortVerified == null)
            .ToListAsync(ct);
        var zeroEffortRecordIds = zeroEffortRecords.Select(r => r.Id).ToHashSet();

        var zeroEnrollmentCourses = await _context.Courses
            .AsTracking(tracking)
            .Where(c => c.TermCode == termCode && c.Enrollment == 0)
            .ToListAsync(ct);
        var zeroEnrollmentCourseIds = zeroEnrollmentCourses.Select(c => c.Id).ToList();

        var zeroEnrollmentRecords = await _context.Records
            .AsNoTracking()
            .Where(r => EF.Parameter(zeroEnrollmentCourseIds).Contains(r.CourseId))
            .Select(r => new { r.Id, r.CourseId })
            .ToListAsync(ct);
        var courseIdsWithEffort = zeroEnrollmentRecords
            .Where(r => !zeroEffortRecordIds.Contains(r.Id))
            .Select(r => r.CourseId)
            .ToHashSet();

        var relationships = await _context.CourseRelationships
            .AsTracking(tracking)
            .Where(r => r.ParentCourse.TermCode == termCode)
            .ToListAsync(ct);

        // Effort belongs on the parent, so a child of a parent with effort counts as having effort
        var parentIds = relationships.Select(r => r.ParentCourseId).Distinct().ToList();
        var parentRecords = await _context.Records
            .AsNoTracking()
            .Where(r => EF.Parameter(parentIds).Contains(r.CourseId))
            .Select(r => new { r.Id, r.CourseId })
            .ToListAsync(ct);
        var parentIdsWithEffort = parentRecords
            .Where(r => !zeroEffortRecordIds.Contains(r.Id))
            .Select(r => r.CourseId)
            .ToHashSet();
        courseIdsWithEffort.UnionWith(relationships
            .Where(r => parentIdsWithEffort.Contains(r.ParentCourseId))
            .Select(r => r.ChildCourseId));

        // The generic R-course never has enrollment, so its effort cannot block closing
        var blockingCourses = zeroEnrollmentCourses
            .Where(c => courseIdsWithEffort.Contains(c.Id) && !_classificationService.IsGenericRCourse(c.Crn))
            .ToList();
        var emptyCourses = zeroEnrollmentCourses
            .Where(c => !courseIdsWithEffort.Contains(c.Id))
            .ToList();
        var emptyCourseIds = emptyCourses.Select(c => c.Id).ToHashSet();

        // Keep an empty parent whose children survive, so the cross-listing stays intact
        var parentIdsToKeep = relationships
            .Where(r => !emptyCourseIds.Contains(r.ChildCourseId))
            .Select(r => r.ParentCourseId)
            .ToHashSet();
        var coursesToDelete = emptyCourses.Where(c => !parentIdsToKeep.Contains(c.Id)).ToList();
        var courseIdsToDelete = coursesToDelete.Select(c => c.Id).ToHashSet();

        return new CloseCleanup(
            zeroEffortRecords,
            coursesToDelete,
            relationships
                .Where(r => courseIdsToDelete.Contains(r.ParentCourseId) || courseIdsToDelete.Contains(r.ChildCourseId))
                .ToList(),
            blockingCourses);
    }

    private static string FormatCourseCode(EffortCourse course) =>
        $"{course.SubjCode.Trim()} {course.CrseNumb.Trim()}-{course.SeqNumb.Trim()}";

    private sealed record CloseCleanup(
        List<EffortRecord> Records,
        List<EffortCourse> Courses,
        List<CourseRelationship> Relationships,
        List<EffortCourse> BlockingCourses);

    public async Task<List<AvailableTermDto>> GetAvailableTermsAsync(CancellationToken ct = default)
    {
        var existingTermCodes = await _context.Terms
            .Select(t => t.TermCode)
            .ToListAsync(ct);

        var availableTerms = await _viperContext.Terms
            .AsNoTracking()
            .Where(t => t.StartDate > DateTime.Today)
            .Where(t => !EF.Parameter(existingTermCodes).Contains(t.TermCode))
            .Where(t => t.TermCode != Term.FacilityScheduleTermCode)
            .OrderBy(t => t.TermCode)
            .Select(t => new AvailableTermDto
            {
                TermCode = t.TermCode,
                TermName = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate
            })
            .ToListAsync(ct);

        return availableTerms;
    }
}
