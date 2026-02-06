using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for rolling over percent assignments to a new academic year during harvest.
/// </summary>
public class PercentRolloverService : IPercentRolloverService
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly IEffortAuditService _auditService;
    private readonly ILogger<PercentRolloverService> _logger;

    public PercentRolloverService(
        EffortDbContext context,
        VIPERContext viperContext,
        IEffortAuditService auditService,
        ILogger<PercentRolloverService> logger)
    {
        _context = context;
        _viperContext = viperContext;
        _auditService = auditService;
        _logger = logger;
    }

    public bool ShouldRollover(int termCode)
    {
        var termType = termCode % 100;
        // Fall Semester (9) or Fall Quarter (10) only
        return termType == 9 || termType == 10;
    }

    public async Task<PercentRolloverPreviewDto> GetRolloverPreviewAsync(int termCode, CancellationToken ct = default)
    {
        var result = new PercentRolloverPreviewDto();

        // Fall 2025 (term 202509) -> source AY ending 2025, target AY ending 2026
        var termYear = termCode / 100;
        result.SourceAcademicYear = termYear;
        result.TargetAcademicYear = termYear + 1;
        result.SourceAcademicYearDisplay = $"{termYear - 1}-{termYear}";
        result.TargetAcademicYearDisplay = $"{termYear}-{termYear + 1}";

        // Boundary dates (use date range to avoid .Date client evaluation issues)
        var june30Start = new DateTime(termYear, 6, 30, 0, 0, 0, DateTimeKind.Local);
        var july1Start = new DateTime(termYear, 7, 1, 0, 0, 0, DateTimeKind.Local);
        result.OldEndDate = june30Start;
        result.NewStartDate = july1Start;
        result.NewEndDate = new DateTime(termYear + 1, 6, 30, 0, 0, 0, DateTimeKind.Local);

        // Find assignments ending on June 30 of source year (any time on that day)
        var assignments = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .Where(p => p.EndDate.HasValue
                && p.EndDate.Value >= june30Start
                && p.EndDate.Value < july1Start)
            .ToListAsync(ct);

        // Get existing target-year records to exclude already-rolled items (idempotency)
        // Key includes PercentageValue and Comment to distinguish legitimate distinct assignments
        var existingTargetRecords = await _context.Percentages
            .AsNoTracking()
            .Where(p => p.StartDate >= july1Start && p.StartDate < july1Start.AddDays(1))
            .Select(p => new { p.PersonId, p.PercentAssignTypeId, p.UnitId, p.Modifier, p.Compensated, p.PercentageValue, p.Comment, p.EndDate })
            .ToListAsync(ct);

        var existingKeys = existingTargetRecords
            .Where(r => r.EndDate == result.NewEndDate)
            .Select(r => (r.PersonId, r.PercentAssignTypeId, r.UnitId, r.Modifier, r.Compensated, r.PercentageValue, r.Comment))
            .ToHashSet();

        // Filter out assignments that already have target-year records
        var assignmentsToRollover = assignments
            .Where(a => !existingKeys.Contains((a.PersonId, a.PercentAssignTypeId, a.UnitId, a.Modifier, a.Compensated, a.PercentageValue, a.Comment)))
            .ToList();

        // Track assignments that already have successors (already rolled)
        var alreadyRolledAssignments = assignments
            .Where(a => existingKeys.Contains((a.PersonId, a.PercentAssignTypeId, a.UnitId, a.Modifier, a.Compensated, a.PercentageValue, a.Comment)))
            .ToList();

        // Get (PersonId, PercentAssignTypeId) combinations that were manually edited/deleted after harvest
        // If the term has been harvested, exclude assignments where someone made manual changes
        var excludedByAudit = new List<Percentage>();
        var term = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term?.HarvestedDate != null)
        {
            var excludedCombinations = await _auditService.GetPostHarvestPercentChangesAsync(term.HarvestedDate.Value, ct);
            if (excludedCombinations.Count > 0)
            {
                excludedByAudit = assignmentsToRollover
                    .Where(a => excludedCombinations.Contains((a.PersonId, a.PercentAssignTypeId)))
                    .ToList();
                assignmentsToRollover = assignmentsToRollover
                    .Where(a => !excludedCombinations.Contains((a.PersonId, a.PercentAssignTypeId)))
                    .ToList();
            }
        }

        // Get person names for all lists
        var personIds = assignmentsToRollover.Select(a => a.PersonId)
            .Union(alreadyRolledAssignments.Select(a => a.PersonId))
            .Union(excludedByAudit.Select(a => a.PersonId))
            .Distinct()
            .ToList();
        var persons = await _viperContext.People
            .AsNoTracking()
            .Where(p => personIds.Contains(p.PersonId))
            .ToDictionaryAsync(p => p.PersonId, ct);

        result.Assignments = assignmentsToRollover.Select(a => MapToPreviewItem(a, persons, result)).ToList();
        result.ExistingAssignments = alreadyRolledAssignments.Select(a => MapToPreviewItem(a, persons, result)).ToList();
        result.ExcludedByAudit = excludedByAudit.Select(a => MapToPreviewItem(a, persons, result)).ToList();

        result.IsRolloverApplicable = result.Assignments.Count > 0;
        return result;
    }

    private static PercentRolloverItemPreview MapToPreviewItem(
        Percentage a,
        Dictionary<int, Person> persons,
        PercentRolloverPreviewDto result)
    {
        var person = persons.GetValueOrDefault(a.PersonId);
        return new PercentRolloverItemPreview
        {
            SourcePercentageId = a.Id,
            PersonId = a.PersonId,
            PersonName = person != null ? $"{person.LastName}, {person.FirstName}" : "Unknown",
            MothraId = person?.MothraId ?? "",
            TypeName = a.PercentAssignType.Name,
            TypeClass = a.PercentAssignType.Class,
            PercentageValue = a.PercentageValue,
            UnitName = a.Unit?.Name,
            Modifier = a.Modifier,
            Compensated = a.Compensated,
            CurrentEndDate = a.EndDate!.Value,
            ProposedStartDate = result.NewStartDate,
            ProposedEndDate = result.NewEndDate
        };
    }

    public async Task<int> ExecuteRolloverAsync(int termCode, int modifiedBy, CancellationToken ct = default)
    {
        // Guard: Only Fall terms should have rollover
        if (!ShouldRollover(termCode))
        {
            _logger.LogInformation("Skipping percent rollover for non-Fall term {TermCode}", termCode);
            return 0;
        }

        // Get fresh preview (includes idempotency check)
        var preview = await GetRolloverPreviewAsync(termCode, ct);
        if (!preview.IsRolloverApplicable)
        {
            _logger.LogInformation("No percent assignments to rollover for term {TermCode}", termCode);
            return 0;
        }

        var sourceIds = preview.Assignments.Select(a => a.SourcePercentageId).ToList();
        var sourceRecords = await _context.Percentages
            .Where(p => sourceIds.Contains(p.Id))
            .ToListAsync(ct);

        var created = 0;
        foreach (var source in sourceRecords)
        {
            var newRecord = new Percentage
            {
                PersonId = source.PersonId,
                AcademicYear = preview.TargetAcademicYearDisplay,  // String: "2025-2026"
                PercentageValue = source.PercentageValue,
                PercentAssignTypeId = source.PercentAssignTypeId,
                UnitId = source.UnitId,
                Modifier = source.Modifier,
                Comment = source.Comment,
                StartDate = preview.NewStartDate,
                EndDate = preview.NewEndDate,
                Compensated = source.Compensated,
                ModifiedDate = DateTime.Now,
                ModifiedBy = modifiedBy
            };
            _context.Percentages.Add(newRecord);
            created++;
        }

        // NOTE: SaveChangesAsync is called by HarvestService after this method returns
        // This keeps rollover within the same transaction as the rest of harvest

        // Audit
        _auditService.AddImportAudit(termCode, EffortAuditActions.RolloverPercentAssignments,
            $"Rolled over {created} percent assignments from {preview.SourceAcademicYearDisplay} to {preview.TargetAcademicYearDisplay}");

        _logger.LogInformation("Rolled over {Count} percent assignments for term {TermCode}: {Source} -> {Target}",
            created, termCode, preview.SourceAcademicYearDisplay, preview.TargetAcademicYearDisplay);

        return created;
    }

    public async Task<int> ExecuteRolloverWithProgressAsync(
        int termCode,
        int modifiedBy,
        ChannelWriter<RolloverProgressEvent> progressChannel,
        CancellationToken ct = default)
    {
        // Guard: Only Fall terms should have rollover
        if (!ShouldRollover(termCode))
        {
            _logger.LogInformation("Skipping percent rollover for non-Fall term {TermCode}", termCode);
            var noOpResult = new RolloverResultDto
            {
                Success = true,
                AssignmentsCreated = 0,
                SourceAcademicYear = "",
                TargetAcademicYear = ""
            };
            await progressChannel.WriteAsync(RolloverProgressEvent.Complete(noOpResult), ct);
            progressChannel.Complete();
            return 0;
        }

        await progressChannel.WriteAsync(RolloverProgressEvent.Preparing(), ct);

        // Get fresh preview (includes idempotency check)
        var preview = await GetRolloverPreviewAsync(termCode, ct);
        if (!preview.IsRolloverApplicable)
        {
            _logger.LogInformation("No percent assignments to rollover for term {TermCode}", termCode);
            var noRolloverResult = new RolloverResultDto
            {
                Success = true,
                AssignmentsCreated = 0,
                SourceAcademicYear = preview.SourceAcademicYearDisplay,
                TargetAcademicYear = preview.TargetAcademicYearDisplay
            };
            await progressChannel.WriteAsync(RolloverProgressEvent.Complete(noRolloverResult), ct);
            progressChannel.Complete();
            return 0;
        }

        var sourceIds = preview.Assignments.Select(a => a.SourcePercentageId).ToList();
        var sourceRecords = await _context.Percentages
            .Where(p => sourceIds.Contains(p.Id))
            .ToListAsync(ct);

        var total = sourceRecords.Count;
        var created = 0;
        foreach (var source in sourceRecords)
        {
            ct.ThrowIfCancellationRequested();

            var newRecord = new Percentage
            {
                PersonId = source.PersonId,
                AcademicYear = preview.TargetAcademicYearDisplay,
                PercentageValue = source.PercentageValue,
                PercentAssignTypeId = source.PercentAssignTypeId,
                UnitId = source.UnitId,
                Modifier = source.Modifier,
                Comment = source.Comment,
                StartDate = preview.NewStartDate,
                EndDate = preview.NewEndDate,
                Compensated = source.Compensated,
                ModifiedDate = DateTime.Now,
                ModifiedBy = modifiedBy
            };
            _context.Percentages.Add(newRecord);
            created++;

            await progressChannel.WriteAsync(RolloverProgressEvent.Rolling(created, total), ct);
        }

        await progressChannel.WriteAsync(RolloverProgressEvent.Finalizing(), ct);

        // Audit (add before SaveChanges to persist with rollover changes)
        _auditService.AddImportAudit(termCode, EffortAuditActions.RolloverPercentAssignments,
            $"Rolled over {created} percent assignments from {preview.SourceAcademicYearDisplay} to {preview.TargetAcademicYearDisplay}");

        // Save changes (standalone operation, not part of harvest transaction)
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Rolled over {Count} percent assignments for term {TermCode}: {Source} -> {Target}",
            created, termCode, preview.SourceAcademicYearDisplay, preview.TargetAcademicYearDisplay);

        var result = new RolloverResultDto
        {
            Success = true,
            AssignmentsCreated = created,
            SourceAcademicYear = preview.SourceAcademicYearDisplay,
            TargetAcademicYear = preview.TargetAcademicYearDisplay
        };

        await progressChannel.WriteAsync(RolloverProgressEvent.Complete(result), ct);
        progressChannel.Complete();

        return created;
    }
}
