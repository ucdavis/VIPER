using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services.Harvest;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Orchestrator service for harvesting instructor and course data into the effort system.
/// Coordinates execution of harvest phases (CREST, Non-CREST, Clinical).
/// </summary>
public class HarvestService : IHarvestService
{
    private readonly IEnumerable<IHarvestPhase> _phases;
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly CoursesContext _coursesContext;
    private readonly CrestContext _crestContext;
    private readonly AAUDContext _aaudContext;
    private readonly DictionaryContext _dictionaryContext;
    private readonly IEffortAuditService _auditService;
    private readonly ITermService _termService;
    private readonly IInstructorService _instructorService;
    private readonly IRCourseService _rCourseService;
    private readonly IPercentRolloverService _percentRolloverService;
    private readonly ILogger<HarvestService> _logger;

    public HarvestService(
        IEnumerable<IHarvestPhase> phases,
        EffortDbContext context,
        VIPERContext viperContext,
        CoursesContext coursesContext,
        CrestContext crestContext,
        AAUDContext aaudContext,
        DictionaryContext dictionaryContext,
        IEffortAuditService auditService,
        ITermService termService,
        IInstructorService instructorService,
        IRCourseService rCourseService,
        IPercentRolloverService percentRolloverService,
        ILogger<HarvestService> logger)
    {
        _phases = phases;
        _context = context;
        _viperContext = viperContext;
        _coursesContext = coursesContext;
        _crestContext = crestContext;
        _aaudContext = aaudContext;
        _dictionaryContext = dictionaryContext;
        _auditService = auditService;
        _termService = termService;
        _instructorService = instructorService;
        _rCourseService = rCourseService;
        _percentRolloverService = percentRolloverService;
        _logger = logger;
    }

    public async Task<HarvestPreviewDto> GeneratePreviewAsync(int termCode, CancellationToken ct = default)
    {
        var harvestContext = CreateHarvestContext(termCode, modifiedBy: 0);

        harvestContext.Preview.TermCode = termCode;
        harvestContext.Preview.TermName = _termService.GetTermName(termCode);

        // Run all phases in order
        foreach (var phase in _phases.Where(p => p.ShouldExecute(termCode)).OrderBy(p => p.Order))
        {
            await phase.GeneratePreviewAsync(harvestContext, ct);
        }

        // Calculate summary
        CalculateSummary(harvestContext);

        // Detect existing and removed items
        await DetectExistingAndRemovedItemsAsync(harvestContext, ct);

        // Generate percent assignment rollover preview (Fall terms only)
        if (_percentRolloverService.ShouldRollover(termCode))
        {
            harvestContext.Preview.PercentRollover =
                await _percentRolloverService.GetRolloverPreviewAsync(termCode, ct);
        }

        return harvestContext.Preview;
    }

    public async Task<HarvestResultDto> ExecuteHarvestAsync(int termCode, int modifiedBy, CancellationToken ct = default)
    {
        var result = new HarvestResultDto { TermCode = termCode };

        // Verify term exists
        var term = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            result.Success = false;
            result.ErrorMessage = $"Term {termCode} not found.";
            return result;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var harvestContext = CreateHarvestContext(termCode, modifiedBy);

            // Phase 0: Clear existing data
            _logger.LogInformation("Clearing existing data for term {TermCode}", termCode);
            await ClearExistingDataAsync(termCode, ct);

            // Generate preview data and execute each phase
            foreach (var phase in _phases.Where(p => p.ShouldExecute(termCode)).OrderBy(p => p.Order))
            {
                await phase.GeneratePreviewAsync(harvestContext, ct);
            }

            // Validate data before import
            var validationError = ValidatePreviewData(harvestContext);
            if (validationError != null)
            {
                result.Success = false;
                result.ErrorMessage = validationError;
                await transaction.RollbackAsync(ct);
                return result;
            }

            // Execute phases in order
            foreach (var phase in _phases.Where(p => p.ShouldExecute(termCode)).OrderBy(p => p.Order))
            {
                await phase.ExecuteAsync(harvestContext, ct);
                await _context.SaveChangesAsync(ct);

                // Write audit entries for created records
                foreach (var (record, preview) in harvestContext.CreatedRecords)
                {
                    _auditService.AddHarvestRecordAudit(
                        record.Id, termCode, preview.MothraId,
                        preview.CourseCode, preview.EffortType, preview.Hours, preview.Weeks);
                }
                harvestContext.CreatedRecords.Clear();
            }

            // Phase 7: Generate R-courses for eligible instructors (post-harvest step)
            _logger.LogInformation("Generating R-courses for eligible instructors in term {TermCode}", termCode);
            await GenerateRCoursesForEligibleInstructorsAsync(termCode, modifiedBy, ct);

            // Execute percent assignment rollover (Fall terms only)
            if (_percentRolloverService.ShouldRollover(termCode))
            {
                await _percentRolloverService.ExecuteRolloverAsync(termCode, modifiedBy, ct);
            }

            // Update term status
            await UpdateTermStatusAsync(termCode, ct);

            // Add import audit
            var summaryText = BuildSummaryText(harvestContext);
            _auditService.AddImportAudit(termCode, EffortAuditActions.ImportEffort, summaryText);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // Calculate summary for result
            CalculateSummary(harvestContext);

            result.Success = true;
            result.HarvestedDate = DateTime.Now;
            result.Summary = harvestContext.Preview.Summary;
            result.Warnings = harvestContext.Warnings;

            _logger.LogInformation("Harvest completed for term {TermCode}: {Summary}", termCode, summaryText);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Database error during harvest for term {TermCode}", termCode);
            result.Success = false;
            result.ErrorMessage = "Database error during harvest. Please try again.";
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Operation error during harvest for term {TermCode}", termCode);
            result.Success = false;
            result.ErrorMessage = "Operation error during harvest. Please contact support.";
        }
        catch (ArgumentNullException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Null argument error during harvest for term {TermCode}", termCode);
            result.Success = false;
            result.ErrorMessage = "Harvest failed due to invalid input. Please contact support.";
        }

        return result;
    }

    public async Task ExecuteHarvestWithProgressAsync(
        int termCode,
        int modifiedBy,
        System.Threading.Channels.ChannelWriter<HarvestProgressEvent> progressChannel,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var harvestContext = CreateHarvestContext(termCode, modifiedBy);

            // Phase 0: Clear existing data (5% of progress)
            await progressChannel.WriteAsync(HarvestProgressEvent.Clearing(), ct);
            _logger.LogInformation("Clearing existing data for term {TermCode}", termCode);
            await ClearExistingDataAsync(termCode, ct);

            // Generate preview data (5-10% of progress)
            await progressChannel.WriteAsync(new HarvestProgressEvent
            {
                Phase = "preview",
                Progress = 0.08,
                Message = "Analyzing data sources..."
            }, ct);

            foreach (var phase in _phases.Where(p => p.ShouldExecute(termCode)).OrderBy(p => p.Order))
            {
                await phase.GeneratePreviewAsync(harvestContext, ct);
            }

            // Validate
            var validationError = ValidatePreviewData(harvestContext);
            if (validationError != null)
            {
                await transaction.RollbackAsync(ct);
                await progressChannel.WriteAsync(HarvestProgressEvent.Failed(validationError), ct);
                return;
            }

            // Calculate totals for progress reporting (deduplicate instructors by MothraId)
            var totalInstructors = GetAllInstructors(harvestContext.Preview)
                .DistinctBy(p => p.MothraId)
                .Count();
            var totalCourses = harvestContext.Preview.CrestCourses.Count +
                               harvestContext.Preview.NonCrestCourses.Count(c => c.Source != EffortConstants.SourceInCrest) +
                               harvestContext.Preview.ClinicalCourses.Count;
            var totalRecords = harvestContext.Preview.CrestEffort.Count +
                               harvestContext.Preview.NonCrestEffort.Count +
                               harvestContext.Preview.ClinicalEffort.Count;

            // Set up progress tracking in context
            harvestContext.TotalInstructors = totalInstructors;
            harvestContext.TotalCourses = totalCourses;
            harvestContext.TotalRecords = totalRecords;

            // Set up progress callback for live updates during phase execution
            harvestContext.ProgressCallback = async (currentInst, totalInst, currentCourses, totalCrs, currentRecs, totalRecs, phaseName) =>
            {
                // Calculate overall progress: 10% to 90% based on total items imported
                var totalItems = totalInst + totalCrs + totalRecs;
                var currentItems = currentInst + currentCourses + currentRecs;
                var itemProgress = totalItems > 0 ? (double)currentItems / totalItems : 0;
                var overallProgress = 0.10 + (0.80 * itemProgress);

                await progressChannel.WriteAsync(new HarvestProgressEvent
                {
                    Phase = phaseName.ToLowerInvariant(),
                    Progress = overallProgress,
                    Message = $"Importing {phaseName} data...",
                    Detail = $"{currentInst}/{totalInst} instructors, {currentCourses}/{totalCrs} courses, {currentRecs}/{totalRecs} records"
                }, ct);
            };

            // Execute phases
            foreach (var phase in _phases.Where(p => p.ShouldExecute(termCode)).OrderBy(p => p.Order))
            {
                await phase.ExecuteAsync(harvestContext, ct);
                await _context.SaveChangesAsync(ct);

                // Force final progress report for this phase
                await harvestContext.ForceReportProgressAsync(phase.PhaseName);

                // Write audit entries
                foreach (var (record, preview) in harvestContext.CreatedRecords)
                {
                    _auditService.AddHarvestRecordAudit(
                        record.Id, termCode, preview.MothraId,
                        preview.CourseCode, preview.EffortType, preview.Hours, preview.Weeks);
                }
                harvestContext.CreatedRecords.Clear();
            }

            // Phase 7: Generate R-courses for eligible instructors (90-95% progress)
            await progressChannel.WriteAsync(new HarvestProgressEvent
            {
                Phase = "rcourse",
                Progress = 0.92,
                Message = "Generating R-courses for eligible instructors..."
            }, ct);
            _logger.LogInformation("Generating R-courses for eligible instructors in term {TermCode}", termCode);
            await GenerateRCoursesForEligibleInstructorsAsync(termCode, modifiedBy, ct);

            // Execute percent assignment rollover (Fall terms only)
            if (_percentRolloverService.ShouldRollover(termCode))
            {
                await _percentRolloverService.ExecuteRolloverAsync(termCode, modifiedBy, ct);
            }

            // Finalize (95% to 100%)
            await progressChannel.WriteAsync(HarvestProgressEvent.Finalizing(), ct);
            await UpdateTermStatusAsync(termCode, ct);

            var summaryText = BuildSummaryText(harvestContext);
            _auditService.AddImportAudit(termCode, EffortAuditActions.ImportEffort, summaryText);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            CalculateSummary(harvestContext);

            var result = new HarvestResultDto
            {
                TermCode = termCode,
                Success = true,
                HarvestedDate = DateTime.Now,
                Summary = harvestContext.Preview.Summary,
                Warnings = harvestContext.Warnings
            };

            _logger.LogInformation("Harvest completed for term {TermCode}: {Summary}", termCode, summaryText);
            await progressChannel.WriteAsync(HarvestProgressEvent.Complete(result), ct);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Database error during harvest for term {TermCode}", termCode);
            await progressChannel.WriteAsync(HarvestProgressEvent.Failed("Database error during harvest. Please try again."), ct);
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Operation error during harvest for term {TermCode}", termCode);
            await progressChannel.WriteAsync(HarvestProgressEvent.Failed("An error occurred during harvest."), ct);
        }
        finally
        {
            progressChannel.Complete();
        }
    }

    #region R-Course Generation

    /// <summary>
    /// Generate R-courses for all eligible instructors in the term.
    /// An instructor is eligible if they have at least one non-R-course effort record.
    /// This must run AFTER all Banner course imports are complete.
    /// Delegates actual R-course creation to IRCourseService for shared logic.
    /// </summary>
    private async Task GenerateRCoursesForEligibleInstructorsAsync(int termCode, int modifiedBy, CancellationToken ct)
    {
        // Find all instructors in the term who have at least one non-R-course effort record
        var eligibleInstructors = await _context.Records
            .AsNoTracking()
            .Where(r => r.TermCode == termCode)
            .Join(_context.Courses.Where(c => c.TermCode == termCode),
                  r => r.CourseId,
                  c => c.Id,
                  (r, c) => new { r.PersonId, c.CrseNumb })
#pragma warning disable S6610 // "EndsWith" overloads that take a "char" should be used
            .Where(x => x.CrseNumb == null || !x.CrseNumb.EndsWith("R")) // TODO(VPR-41): EF Core 10 supports char overload, remove pragma
#pragma warning restore S6610
            .Select(x => x.PersonId)
            .Distinct()
            .ToListAsync(ct);

        _logger.LogInformation("Found {Count} eligible instructors for R-course generation in term {TermCode}",
            eligibleInstructors.Count, termCode);

        foreach (var personId in eligibleInstructors)
        {
            await _rCourseService.CreateRCourseEffortRecordAsync(personId, termCode, modifiedBy, RCourseCreationContext.Harvest, ct);
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Get all instructors from all sources (CREST, Non-CREST, Clinical).
    /// </summary>
    private static IEnumerable<HarvestPersonPreview> GetAllInstructors(HarvestPreviewDto preview)
    {
        return preview.CrestInstructors
            .Concat(preview.NonCrestInstructors)
            .Concat(preview.ClinicalInstructors);
    }

    private HarvestContext CreateHarvestContext(int termCode, int modifiedBy)
    {
        return new HarvestContext
        {
            TermCode = termCode,
            ModifiedBy = modifiedBy,
            EffortContext = _context,
            CrestContext = _crestContext,
            CoursesContext = _coursesContext,
            ViperContext = _viperContext,
            AaudContext = _aaudContext,
            DictionaryContext = _dictionaryContext,
            AuditService = _auditService,
            InstructorService = _instructorService,
            TermService = _termService,
            Logger = _logger
        };
    }

    private async Task ClearExistingDataAsync(int termCode, CancellationToken ct)
    {
        // Check if database supports ExecuteDeleteAsync (in-memory provider does not)
        var isInMemory = _context.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) ?? false;

        if (isInMemory)
        {
            // Fall back to traditional delete for in-memory testing
            var recordsToDelete = await _context.Records.Where(r => r.TermCode == termCode).ToListAsync(ct);
            _context.Records.RemoveRange(recordsToDelete);

            var coursesToDelete = await _context.Courses.Where(c => c.TermCode == termCode).ToListAsync(ct);
            _context.Courses.RemoveRange(coursesToDelete);

            var personsToDelete = await _context.Persons.Where(p => p.TermCode == termCode).ToListAsync(ct);
            _context.Persons.RemoveRange(personsToDelete);

            await _context.SaveChangesAsync(ct);
        }
        else
        {
            // Use efficient bulk delete for production
            await _context.Records.Where(r => r.TermCode == termCode).ExecuteDeleteAsync(ct);
            await _context.Courses.Where(c => c.TermCode == termCode).ExecuteDeleteAsync(ct);
            await _context.Persons.Where(p => p.TermCode == termCode).ExecuteDeleteAsync(ct);
        }

        await _auditService.ClearAuditForTermAsync(termCode, ct);
    }

    private static string? ValidatePreviewData(HarvestContext context)
    {
        // Guard: Abort if CREST courses exist but no effort data was extracted
        if (context.Preview.CrestCourses.Count > 0 && context.Preview.CrestEffort.Count == 0)
        {
            return "CREST effort data is empty; aborting harvest to prevent data loss.";
        }

        // Guard: Abort if CREST instructors exist but no courses
        if (context.Preview.CrestInstructors.Count > 0 && context.Preview.CrestCourses.Count == 0)
        {
            return "CREST instructors found but no courses; possible data extraction issue. Aborting harvest.";
        }

        return null;
    }

    private async Task UpdateTermStatusAsync(int termCode, CancellationToken ct)
    {
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term != null)
        {
            var oldStatus = term.Status;
            term.HarvestedDate = DateTime.Now;

            _auditService.AddTermChangeAudit(termCode, EffortAuditActions.HarvestTerm,
                new { Status = oldStatus },
                new { Status = term.Status, term.HarvestedDate });
        }
    }

    private static void CalculateSummary(HarvestContext context)
    {
        var allInstructors = GetAllInstructors(context.Preview)
            .DistinctBy(p => p.MothraId)
            .ToList();

        var allCourses = context.Preview.CrestCourses
            .Concat(context.Preview.NonCrestCourses.Where(c => c.Source != EffortConstants.SourceInCrest))
            .Concat(context.Preview.ClinicalCourses)
            .DistinctBy(c => string.IsNullOrWhiteSpace(c.Crn)
                ? $"{c.SubjCode}-{c.CrseNumb}-{c.SeqNumb}-{c.Units}"
                : $"CRN:{c.Crn}-{c.Units}")
            .ToList();

        context.Preview.Summary = new HarvestSummary
        {
            TotalInstructors = allInstructors.Count,
            TotalCourses = allCourses.Count,
            TotalEffortRecords = context.Preview.CrestEffort.Count +
                                 context.Preview.NonCrestEffort.Count +
                                 context.Preview.ClinicalEffort.Count
        };
    }

    private static string BuildSummaryText(HarvestContext context)
    {
        var allInstructors = GetAllInstructors(context.Preview)
            .DistinctBy(p => p.MothraId)
            .Count();

        var allCourses = context.Preview.CrestCourses.Count +
                         context.Preview.NonCrestCourses.Count(c => c.Source != EffortConstants.SourceInCrest) +
                         context.Preview.ClinicalCourses.Count;

        var allEffort = context.Preview.CrestEffort.Count +
                        context.Preview.NonCrestEffort.Count +
                        context.Preview.ClinicalEffort.Count;

        return $"Harvested: {allInstructors} instructors, {allCourses} courses, {allEffort} effort records";
    }

    private async Task DetectExistingAndRemovedItemsAsync(HarvestContext context, CancellationToken ct)
    {
        var termCode = context.TermCode;

        // Get existing instructors
        var existingPersonIds = await _context.Persons
            .AsNoTracking()
            .Where(p => p.TermCode == termCode)
            .Select(p => new { p.PersonId, p.FirstName, p.LastName, Department = p.EffortDept, TitleCode = p.EffortTitleCode })
            .ToListAsync(ct);

        var personIdsToLookup = existingPersonIds.Select(p => p.PersonId).ToList();
        var personIdToMothraId = await _viperContext.People
            .AsNoTracking()
            .Where(p => personIdsToLookup.Contains(p.PersonId))
            .Select(p => new { p.PersonId, p.MothraId })
            .ToDictionaryAsync(p => p.PersonId, p => p.MothraId ?? "", ct);

        var existingInstructors = existingPersonIds
            .Select(p => new
            {
                MothraId = personIdToMothraId.GetValueOrDefault(p.PersonId, ""),
                p.FirstName,
                p.LastName,
                p.Department,
                p.TitleCode
            })
            .Where(p => !string.IsNullOrEmpty(p.MothraId))
            .ToList();

        var existingCourses = await _context.Courses
            .AsNoTracking()
            .Where(c => c.TermCode == termCode)
            .Select(c => new { c.Crn, c.SubjCode, c.CrseNumb, c.SeqNumb, c.Enrollment, c.Units, c.CustDept })
            .ToListAsync(ct);

        var existingMothraIds = existingInstructors.Select(i => i.MothraId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCourseKeys = existingCourses
            .Select(c => BuildExistingCourseKey(c.Crn, c.SubjCode, c.CrseNumb, c.SeqNumb, c.Units))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Set IsNew flag for instructors
        var harvestMothraIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var instructor in GetAllInstructors(context.Preview))
        {
            instructor.IsNew = !existingMothraIds.Contains(instructor.MothraId);
            harvestMothraIds.Add(instructor.MothraId);
        }

        // Set IsNew flag for courses
        var harvestCourseKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var course in context.Preview.CrestCourses
            .Concat(context.Preview.NonCrestCourses)
            .Concat(context.Preview.ClinicalCourses))
        {
            var courseKey = BuildExistingCourseKey(course.Crn, course.SubjCode, course.CrseNumb, course.SeqNumb, course.Units);
            course.IsNew = !existingCourseKeys.Contains(courseKey);
            harvestCourseKeys.Add(courseKey);
        }

        // Find removed instructors
        foreach (var existing in existingInstructors.Where(i => !harvestMothraIds.Contains(i.MothraId)))
        {
            context.Preview.RemovedInstructors.Add(new HarvestPersonPreview
            {
                MothraId = existing.MothraId,
                FullName = $"{existing.LastName}, {existing.FirstName}",
                FirstName = existing.FirstName,
                LastName = existing.LastName,
                Department = existing.Department,
                TitleCode = existing.TitleCode,
                Source = EffortConstants.SourceExisting
            });
        }

        // Find removed courses (exclude the auto-generated generic R-course since it's recreated post-harvest)
        foreach (var existing in existingCourses.Where(c =>
            !harvestCourseKeys.Contains(BuildExistingCourseKey(c.Crn, c.SubjCode, c.CrseNumb, c.SeqNumb, c.Units))
            && !string.Equals(c.Crn, "RESID", StringComparison.OrdinalIgnoreCase)))
        {
            context.Preview.RemovedCourses.Add(new HarvestCoursePreview
            {
                Crn = existing.Crn,
                SubjCode = existing.SubjCode,
                CrseNumb = existing.CrseNumb,
                SeqNumb = existing.SeqNumb,
                Enrollment = existing.Enrollment,
                Units = existing.Units,
                CustDept = existing.CustDept,
                Source = EffortConstants.SourceExisting
            });
        }

        // Set IsNew flag for effort records
        var existingRecords = await _context.Records
            .AsNoTracking()
            .Where(r => r.TermCode == termCode)
            .Select(r => new { r.PersonId, r.Crn, r.EffortTypeId })
            .ToListAsync(ct);

        var existingRecordKeys = existingRecords
            .Select(r => $"{personIdToMothraId.GetValueOrDefault(r.PersonId, "")}:{r.Crn}:{r.EffortTypeId}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var record in context.Preview.CrestEffort
            .Concat(context.Preview.NonCrestEffort)
            .Concat(context.Preview.ClinicalEffort))
        {
            var recordKey = $"{record.MothraId}:{record.Crn}:{record.EffortType}";
            record.IsNew = !existingRecordKeys.Contains(recordKey);
        }

        // Check for existing data warning
        var existingRecordCount = existingRecords.Count;

        if (existingRecordCount > 0 || existingInstructors.Count != 0 || existingCourses.Count != 0)
        {
            context.Preview.Warnings.Add(new HarvestWarning
            {
                Phase = "",
                Message = "Existing data will be replaced",
                Details = $"This term has existing data ({existingInstructors.Count} instructors, {existingCourses.Count} courses, {existingRecordCount} effort records) that will be permanently deleted when harvest is confirmed."
            });
        }
    }

    private static string BuildExistingCourseKey(string? crn, string subjCode, string crseNumb, string seqNumb, decimal units)
    {
        return string.IsNullOrWhiteSpace(crn)
            ? $"{subjCode}{crseNumb}{seqNumb}-{units}"
            : $"CRN:{crn}";
    }

    #endregion
}
