using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.Utilities;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for rolling over percent assignments to a new academic year.
/// The boundary year determines the rollover: assignments ending June 30 of that year
/// are copied to July 1 of that year through June 30 of the next year.
/// </summary>
public class PercentRolloverService : IPercentRolloverService
{
    private readonly EffortDbContext _context;
    private readonly IEffortAuditService _auditService;
    private readonly ILogger<PercentRolloverService> _logger;

    public PercentRolloverService(
        EffortDbContext context,
        IEffortAuditService auditService,
        ILogger<PercentRolloverService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<PercentRolloverPreviewDto> GetRolloverPreviewAsync(int year, CancellationToken ct = default)
    {
        var result = new PercentRolloverPreviewDto();

        result.SourceAcademicYear = year;
        result.TargetAcademicYear = year + 1;
        result.SourceAcademicYearDisplay = $"{year - 1}-{year}";
        result.TargetAcademicYearDisplay = $"{year}-{year + 1}";

        // Boundary dates (use date range to avoid .Date client evaluation issues)
        var june30Start = new DateTime(year, 6, 30, 0, 0, 0, DateTimeKind.Local);
        var july1Start = new DateTime(year, 7, 1, 0, 0, 0, DateTimeKind.Local);
        result.OldEndDate = june30Start;
        result.NewStartDate = july1Start;
        result.NewEndDate = new DateTime(year + 1, 6, 30, 0, 0, 0, DateTimeKind.Local);

        // Find assignments ending on June 30 of source year (any time on that day)
        var assignments = await _context.Percentages
            .AsNoTracking()
            .Include(p => p.PercentAssignType)
            .Include(p => p.Unit)
            .Include(p => p.ViperPerson)
            .Where(p => p.EndDate.HasValue
                && p.EndDate.Value >= june30Start
                && p.EndDate.Value < july1Start)
            .ToListAsync(ct);

        // Get existing target-year records to exclude already-rolled items (idempotency)
        // Key includes PercentageValue and Comment to distinguish legitimate distinct assignments
        var existingKeys = (await _context.Percentages
            .AsNoTracking()
            .Where(p => p.StartDate >= july1Start
                && p.StartDate < july1Start.AddDays(1)
                && p.EndDate == result.NewEndDate)
            .Select(p => new { p.PersonId, p.PercentAssignTypeId, p.UnitId, p.Modifier, p.Compensated, p.PercentageValue, p.Comment })
            .ToListAsync(ct))
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

        // Get (PersonId, PercentAssignTypeId) combinations that were manually edited/deleted after harvest.
        // Look up the Fall Semester term for this boundary year to find its HarvestedDate.
        var excludedByAudit = new List<Percentage>();
        var fallSemesterTermCode = year * 100 + 9;
        var term = await _context.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermCode == fallSemesterTermCode, ct);
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

        result.Assignments = assignmentsToRollover.Select(a => MapToPreviewItem(a, result)).ToList();
        result.ExistingAssignments = alreadyRolledAssignments.Select(a => MapToPreviewItem(a, result)).ToList();
        result.ExcludedByAudit = excludedByAudit.Select(a => MapToPreviewItem(a, result)).ToList();

        result.IsRolloverApplicable = result.Assignments.Count > 0;
        return result;
    }

    private static PercentRolloverItemPreview MapToPreviewItem(
        Percentage a,
        PercentRolloverPreviewDto result)
    {
        return new PercentRolloverItemPreview
        {
            SourcePercentageId = a.Id,
            PersonId = a.PersonId,
            PersonName = a.ViperPerson != null ? $"{a.ViperPerson.LastName}, {a.ViperPerson.FirstName}" : "Unknown",
            MothraId = a.ViperPerson?.MothraId ?? "",
            TypeName = a.PercentAssignType.Name,
            TypeClass = a.PercentAssignType.Class,
            PercentageValue = EffortConstants.ToDisplayPercent(a.PercentageValue),
            UnitName = a.Unit?.Name,
            Modifier = a.Modifier,
            Compensated = a.Compensated,
            CurrentEndDate = a.EndDate!.Value,
            ProposedStartDate = result.NewStartDate,
            ProposedEndDate = result.NewEndDate
        };
    }

    public async Task<int> ExecuteRolloverAsync(int year, int modifiedBy, CancellationToken ct = default)
    {
        // Get fresh preview (includes idempotency check)
        var preview = await GetRolloverPreviewAsync(year, ct);
        if (!preview.IsRolloverApplicable)
        {
            _logger.LogInformation("No percent assignments to rollover for year {Year}", LogSanitizer.SanitizeYear(year));
            return 0;
        }

        var sourceIds = preview.Assignments.Select(a => a.SourcePercentageId).ToList();
        var sourceRecords = await _context.Percentages
            .Where(p => sourceIds.Contains(p.Id))
            .ToListAsync(ct);

        var newRecords = sourceRecords.Select(source => new Percentage
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
        }).ToList();
        _context.Percentages.AddRange(newRecords);
        var created = newRecords.Count;

        _auditService.AddImportAudit(EffortAuditActions.RolloverPercentAssignments,
            $"Rolled over {created} percent assignments from {preview.SourceAcademicYearDisplay} to {preview.TargetAcademicYearDisplay}");

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Rolled over {Count} percent assignments for year {Year}: {Source} -> {Target}",
            created, LogSanitizer.SanitizeYear(year), preview.SourceAcademicYearDisplay, preview.TargetAcademicYearDisplay);

        return created;
    }

    public async Task<int> ExecuteRolloverWithProgressAsync(
        int year,
        int modifiedBy,
        ChannelWriter<RolloverProgressEvent> progressChannel,
        CancellationToken ct = default)
    {
        await progressChannel.WriteAsync(RolloverProgressEvent.Preparing(), ct);

        // Get fresh preview (includes idempotency check)
        var preview = await GetRolloverPreviewAsync(year, ct);
        if (!preview.IsRolloverApplicable)
        {
            _logger.LogInformation("No percent assignments to rollover for year {Year}", LogSanitizer.SanitizeYear(year));
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
        var newRecords = new List<Percentage>(total);
        for (var i = 0; i < sourceRecords.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var source = sourceRecords[i];

            newRecords.Add(new Percentage
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
            });

            await progressChannel.WriteAsync(RolloverProgressEvent.Rolling(i + 1, total), ct);
        }
        _context.Percentages.AddRange(newRecords);
        var created = newRecords.Count;

        await progressChannel.WriteAsync(RolloverProgressEvent.Finalizing(), ct);

        _auditService.AddImportAudit(EffortAuditActions.RolloverPercentAssignments,
            $"Rolled over {created} percent assignments from {preview.SourceAcademicYearDisplay} to {preview.TargetAcademicYearDisplay}");

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Rolled over {Count} percent assignments for year {Year}: {Source} -> {Target}",
            created, LogSanitizer.SanitizeYear(year), preview.SourceAcademicYearDisplay, preview.TargetAcademicYearDisplay);

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
