using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Helpers;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for importing clinical effort data from the Clinical Scheduler system.
/// Extracts clinical rotation data and creates/updates effort records.
/// </summary>
public class ClinicalImportService : IClinicalImportService
{
    private readonly EffortDbContext _context;
    private readonly VIPERContext _viperContext;
    private readonly IEffortAuditService _auditService;
    private readonly ILogger<ClinicalImportService> _logger;

    /// <summary>
    /// Clinical course priority lookup. Lower values = higher priority.
    /// When an instructor is assigned to multiple rotations in the same week,
    /// only the highest priority course is credited.
    /// </summary>
    private static readonly Dictionary<string, int> ClinicalCoursePriority = new()
    {
        ["DVM 453"] = -100, // Comm surgery - highest priority
        ["DVM 491"] = 10,   // SA emergency over ICU
        ["DVM 492"] = 11,
        ["DVM 476"] = 20,   // LA anest over SA anest
        ["DVM 490"] = 21,
        ["DVM 477"] = 30,   // LA radio over SA radio
        ["DVM 494"] = 31,
        ["DVM 482"] = 40,   // Med onc over rad onc
        ["DVM 487"] = 41,
        ["DVM 493"] = 50,   // Internal med: Med A first
        ["DVM 466"] = 51,
        ["DVM 443"] = 52,
        ["DVM 451"] = 60,   // Clin path over anat path
        ["DVM 485"] = 61,
        ["DVM 457"] = 70,   // Equine emergency over S&L
        ["DVM 462"] = 71,
        ["DVM 459"] = 80,   // Equine field over in-house
        ["DVM 460"] = 81,
        ["DVM 447"] = 100,  // EduLead - lowest priority
    };

    public ClinicalImportService(
        EffortDbContext context,
        VIPERContext viperContext,
        IEffortAuditService auditService,
        ILogger<ClinicalImportService> logger)
    {
        _context = context;
        _viperContext = viperContext;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ClinicalImportPreviewDto> GetPreviewAsync(int termCode, ClinicalImportMode mode, CancellationToken ct = default)
    {
        var result = new ClinicalImportPreviewDto
        {
            Mode = mode,
            PreviewGeneratedAt = DateTime.Now
        };

        // Validate term eligibility
        var term = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            result.Warnings.Add($"Term {termCode} not found");
            return result;
        }

        if (!TermValidationHelper.CanImportClinical(term.Status, termCode))
        {
            result.Warnings.Add($"Clinical import not available for term {termCode} (status: {term.Status})");
            return result;
        }

        // Get source data from Clinical Scheduler
        var sourceData = await GetSourceDataAsync(termCode, ct);

        if (sourceData.Count == 0)
        {
            result.Warnings.Add("Source returned 0 rows");
            if (mode == ClinicalImportMode.Sync)
            {
                var existingCount = await GetExistingClinicalRecordCountAsync(termCode, ct);
                if (existingCount > 0)
                {
                    result.Warnings.Add($"Sync will delete all {existingCount} clinical effort records for this term");
                }
            }
        }

        // Get existing clinical records for comparison
        var existingRecords = await GetExistingClinicalRecordsAsync(termCode, ct);

        // Build preview based on mode
        result.Assignments = BuildPreviewAssignments(sourceData, existingRecords, mode);

        // Calculate counts
        result.AddCount = result.Assignments.Count(a => a.Status == "New");
        result.UpdateCount = result.Assignments.Count(a => a.Status == "Update");
        result.DeleteCount = result.Assignments.Count(a => a.Status == "Delete");
        result.SkipCount = result.Assignments.Count(a => a.Status == "Skip");

        // Additional warnings
        if (mode == ClinicalImportMode.Sync && result.DeleteCount > 0)
        {
            result.Warnings.Add($"Sync will delete {result.DeleteCount} records");
        }

        return result;
    }

    public async Task<ClinicalImportResultDto> ExecuteImportAsync(int termCode, ClinicalImportMode mode, int modifiedBy, CancellationToken ct = default)
    {
        // Validate term eligibility
        var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
        if (term == null)
        {
            return new ClinicalImportResultDto { Success = false, ErrorMessage = $"Term {termCode} not found" };
        }

        if (!TermValidationHelper.CanImportClinical(term.Status, termCode))
        {
            return new ClinicalImportResultDto
            {
                Success = false,
                ErrorMessage = $"Clinical import not available for term {termCode} (status: {term.Status})"
            };
        }

        // Get fresh source data
        var sourceData = await GetSourceDataAsync(termCode, ct);
        var existingRecords = await GetExistingClinicalRecordsAsync(termCode, ct);

        // ClearReplace needs a transaction: the intermediate SaveChangesAsync (to flush deletes
        // before inserts) must not commit until everything succeeds.
        await using var transaction = mode == ClinicalImportMode.ClearReplace
            ? await _context.Database.BeginTransactionAsync(ct)
            : null;

        // Execute import based on mode
        var result = await ExecuteImportInternalAsync(sourceData, existingRecords, termCode, mode, modifiedBy, ct);

        // Write audit entry
        _auditService.AddImportAudit(termCode, EffortAuditActions.ImportClinical,
            $"Clinical import ({mode}): {result.RecordsAdded} added, {result.RecordsUpdated} updated, {result.RecordsDeleted} deleted, {result.RecordsSkipped} skipped");

        await _context.SaveChangesAsync(ct);

        if (transaction != null)
        {
            await transaction.CommitAsync(ct);
        }

        return result;
    }

    public async Task<ClinicalImportResultDto> ExecuteImportWithProgressAsync(
        int termCode,
        ClinicalImportMode mode,
        int modifiedBy,
        ChannelWriter<ClinicalImportProgressEvent> progressChannel,
        CancellationToken ct = default)
    {
        try
        {
            await progressChannel.WriteAsync(ClinicalImportProgressEvent.Preparing(), ct);

            // Validate term eligibility
            var term = await _context.Terms.FirstOrDefaultAsync(t => t.TermCode == termCode, ct);
            if (term == null)
            {
                var errorResult = new ClinicalImportResultDto { Success = false, ErrorMessage = $"Term {termCode} not found" };
                await progressChannel.WriteAsync(ClinicalImportProgressEvent.Failed(errorResult.ErrorMessage), ct);
                progressChannel.Complete();
                return errorResult;
            }

            if (!TermValidationHelper.CanImportClinical(term.Status, termCode))
            {
                var errorResult = new ClinicalImportResultDto
                {
                    Success = false,
                    ErrorMessage = $"Clinical import not available for term {termCode} (status: {term.Status})"
                };
                await progressChannel.WriteAsync(ClinicalImportProgressEvent.Failed(errorResult.ErrorMessage), ct);
                progressChannel.Complete();
                return errorResult;
            }

            // Get fresh source data
            var sourceData = await GetSourceDataAsync(termCode, ct);
            var existingRecords = await GetExistingClinicalRecordsAsync(termCode, ct);

            // ClearReplace needs a transaction: the intermediate SaveChangesAsync (to flush deletes
            // before inserts) must not commit until everything succeeds.
            await using var transaction = mode == ClinicalImportMode.ClearReplace
                ? await _context.Database.BeginTransactionAsync(ct)
                : null;

            // Execute import based on mode with progress
            var result = await ExecuteImportWithProgressInternalAsync(
                sourceData, existingRecords, termCode, mode, modifiedBy, progressChannel, ct);

            await progressChannel.WriteAsync(ClinicalImportProgressEvent.Finalizing(), ct);

            // Write audit entry
            _auditService.AddImportAudit(termCode, EffortAuditActions.ImportClinical,
                $"Clinical import ({mode}): {result.RecordsAdded} added, {result.RecordsUpdated} updated, {result.RecordsDeleted} deleted, {result.RecordsSkipped} skipped");

            await _context.SaveChangesAsync(ct);

            if (transaction != null)
            {
                await transaction.CommitAsync(ct);
            }

            _logger.LogInformation(
                "Clinical import completed for term {TermCode}: {Added} added, {Updated} updated, {Deleted} deleted, {Skipped} skipped",
                termCode, result.RecordsAdded, result.RecordsUpdated, result.RecordsDeleted, result.RecordsSkipped);

            await progressChannel.WriteAsync(ClinicalImportProgressEvent.Complete(result), ct);
            progressChannel.Complete();

            return result;
        }
        catch (OperationCanceledException)
        {
            progressChannel.Complete();
            throw;
        }
        catch (DbUpdateException)
        {
            await progressChannel.WriteAsync(ClinicalImportProgressEvent.Failed("A database error occurred during import."), ct);
            progressChannel.Complete();
            throw;
        }
        catch (InvalidOperationException ex)
        {
            await progressChannel.WriteAsync(ClinicalImportProgressEvent.Failed(ex.Message), ct);
            progressChannel.Complete();
            throw;
        }
    }

    /// <summary>
    /// Get clinical schedule data from the Clinical Scheduler view.
    /// Returns aggregated records per instructor per course (week count).
    /// </summary>
    private async Task<List<ClinicalSourceRecord>> GetSourceDataAsync(int termCode, CancellationToken ct)
    {
        // Get week IDs for this term
        var weekIds = await _viperContext.Weeks
            .AsNoTracking()
            .Where(w => w.TermCode == termCode)
            .Select(w => w.WeekId)
            .ToListAsync(ct);

        if (weekIds.Count == 0)
        {
            return [];
        }

        // Get clinical instructor schedules with week info
        var clinicalData = await _viperContext.InstructorSchedules
            .AsNoTracking()
            .Where(s => weekIds.Contains(s.WeekId))
            .Where(s => !string.IsNullOrEmpty(s.SubjCode) && !string.IsNullOrEmpty(s.CrseNumb))
            .Join(_viperContext.Weeks.AsNoTracking(),
                s => s.WeekId,
                w => w.WeekId,
                (s, w) => new
                {
                    s.MothraId,
                    s.WeekId,
                    s.SubjCode,
                    s.CrseNumb,
                    s.RotationName,
                    s.ServiceName,
                    WeekStart = w.DateStart,
                    PersonName = s.LastName + ", " + s.FirstName
                })
            .ToListAsync(ct);

        // Group by person and week to track courses per week (for priority resolution)
        var personWeekCourses = new Dictionary<string, Dictionary<int, List<string>>>();

        foreach (var schedule in clinicalData.Where(s => !string.IsNullOrEmpty(s.MothraId)))
        {
            if (!personWeekCourses.ContainsKey(schedule.MothraId))
            {
                personWeekCourses[schedule.MothraId] = [];
            }

            if (!personWeekCourses[schedule.MothraId].ContainsKey(schedule.WeekId))
            {
                personWeekCourses[schedule.MothraId][schedule.WeekId] = [];
            }

            var courseKey = $"{schedule.SubjCode} {schedule.CrseNumb}";
            if (!personWeekCourses[schedule.MothraId][schedule.WeekId].Contains(courseKey))
            {
                personWeekCourses[schedule.MothraId][schedule.WeekId].Add(courseKey);
            }
        }

        // Build detailed records with priority resolution
        var result = new List<ClinicalSourceRecord>();

        foreach (var schedule in clinicalData.Where(s =>
            !string.IsNullOrEmpty(s.MothraId) && !string.IsNullOrEmpty(s.SubjCode)))
        {
            var courseKey = $"{schedule.SubjCode} {schedule.CrseNumb}";

            // Check if this course is the priority for this person/week
            var weekCourses = personWeekCourses[schedule.MothraId][schedule.WeekId];
            var priorityCourse = GetPriorityCourse(weekCourses);

            if (courseKey != priorityCourse) continue;

            result.Add(new ClinicalSourceRecord
            {
                MothraId = schedule.MothraId,
                WeekId = schedule.WeekId,
                WeekStart = schedule.WeekStart,
                SubjCode = schedule.SubjCode ?? "",
                CrseNumb = schedule.CrseNumb ?? "",
                CourseKey = courseKey,
                RotationName = schedule.RotationName ?? "",
                ServiceName = schedule.ServiceName ?? "",
                PersonName = schedule.PersonName ?? ""
            });
        }

        return result;
    }

    /// <summary>
    /// Get existing clinical effort records for comparison.
    /// </summary>
    private async Task<List<ExistingClinicalRecord>> GetExistingClinicalRecordsAsync(int termCode, CancellationToken ct)
    {
        var existingRecords = await _context.Records
            .AsNoTracking()
            .Include(r => r.Course)
            .Include(r => r.Person)
            .Where(r => r.TermCode == termCode)
            .Where(r => r.EffortTypeId == ClinicalEffortTypes.Clinical)
            .Select(r => new ExistingClinicalRecord
            {
                Id = r.Id,
                PersonId = r.PersonId,
                MothraId = _context.ViperPersons
                    .Where(p => p.PersonId == r.PersonId)
                    .Select(p => p.MothraId)
                    .FirstOrDefault() ?? "",
                CourseId = r.CourseId,
                SubjCode = r.Course.SubjCode,
                CrseNumb = r.Course.CrseNumb,
                Weeks = r.Weeks ?? 0,
                PersonName = r.Person.LastName + ", " + r.Person.FirstName
            })
            .ToListAsync(ct);

        return existingRecords;
    }

    private async Task<int> GetExistingClinicalRecordCountAsync(int termCode, CancellationToken ct)
    {
        return await _context.Records
            .AsNoTracking()
            .Where(r => r.TermCode == termCode)
            .Where(r => r.EffortTypeId == ClinicalEffortTypes.Clinical)
            .CountAsync(ct);
    }

    /// <summary>
    /// Build preview assignments based on source data, existing records, and import mode.
    /// </summary>
    private static List<ClinicalAssignmentPreview> BuildPreviewAssignments(
        List<ClinicalSourceRecord> sourceData,
        List<ExistingClinicalRecord> existingRecords,
        ClinicalImportMode mode)
    {
        var assignments = new List<ClinicalAssignmentPreview>();

        // Aggregate source data by person+course (normalize keys to uppercase for case-insensitive matching)
        var sourceAggregated = sourceData
            .GroupBy(s => (MothraId: s.MothraId.ToUpperInvariant(), CourseKey: s.CourseKey.ToUpperInvariant()))
            .Select(g => new
            {
                g.Key.MothraId,
                g.Key.CourseKey,
                Weeks = g.Count(),
                FirstRecord = g.First()
            })
            .ToList();

        // Create lookup for existing records by MothraId+CourseKey (normalize to uppercase, trim whitespace)
        var existingLookup = existingRecords
            .GroupBy(e => (MothraId: e.MothraId.ToUpperInvariant(), CourseKey: BuildNormalizedCourseKey(e.SubjCode, e.CrseNumb)))
            .ToDictionary(g => g.Key, g => g.First());

        // Source data keys for sync mode delete detection
        var sourceKeys = sourceAggregated
            .Select(s => (s.MothraId, s.CourseKey))
            .ToHashSet();

        switch (mode)
        {
            case ClinicalImportMode.AddNewOnly:
                foreach (var source in sourceAggregated)
                {
                    var key = (source.MothraId, source.CourseKey);
                    var hasExisting = existingLookup.TryGetValue(key, out var existing);

                    assignments.Add(new ClinicalAssignmentPreview
                    {
                        Status = hasExisting ? "Skip" : "New",
                        ExistingRecordId = hasExisting && existing != null ? existing.Id : null,
                        MothraId = source.FirstRecord.MothraId,
                        InstructorName = source.FirstRecord.PersonName,
                        CourseNumber = source.FirstRecord.CourseKey,
                        Weeks = hasExisting && existing != null ? existing.Weeks : source.Weeks
                    });
                }
                break;

            case ClinicalImportMode.ClearReplace:
                foreach (var existing in existingRecords)
                {
                    assignments.Add(new ClinicalAssignmentPreview
                    {
                        Status = "Delete",
                        ExistingRecordId = existing.Id,
                        MothraId = existing.MothraId,
                        InstructorName = existing.PersonName,
                        CourseNumber = $"{existing.SubjCode} {existing.CrseNumb}",
                        Weeks = existing.Weeks
                    });
                }

                foreach (var source in sourceAggregated)
                {
                    assignments.Add(new ClinicalAssignmentPreview
                    {
                        Status = "New",
                        MothraId = source.FirstRecord.MothraId,
                        InstructorName = source.FirstRecord.PersonName,
                        CourseNumber = source.FirstRecord.CourseKey,
                        Weeks = source.Weeks
                    });
                }
                break;

            case ClinicalImportMode.Sync:
                foreach (var source in sourceAggregated)
                {
                    var key = (source.MothraId, source.CourseKey);
                    if (existingLookup.TryGetValue(key, out var existing))
                    {
                        var status = existing.Weeks != source.Weeks ? "Update" : "Skip";
                        assignments.Add(new ClinicalAssignmentPreview
                        {
                            Status = status,
                            ExistingRecordId = existing.Id,
                            MothraId = source.FirstRecord.MothraId,
                            InstructorName = source.FirstRecord.PersonName,
                            CourseNumber = source.FirstRecord.CourseKey,
                            Weeks = source.Weeks
                        });
                    }
                    else
                    {
                        assignments.Add(new ClinicalAssignmentPreview
                        {
                            Status = "New",
                            MothraId = source.FirstRecord.MothraId,
                            InstructorName = source.FirstRecord.PersonName,
                            CourseNumber = source.FirstRecord.CourseKey,
                            Weeks = source.Weeks
                        });
                    }
                }

                foreach (var existing in existingRecords)
                {
                    var key = (existing.MothraId.ToUpperInvariant(), CourseKey: BuildNormalizedCourseKey(existing.SubjCode, existing.CrseNumb));
                    if (!sourceKeys.Contains(key))
                    {
                        assignments.Add(new ClinicalAssignmentPreview
                        {
                            Status = "Delete",
                            ExistingRecordId = existing.Id,
                            MothraId = existing.MothraId,
                            InstructorName = existing.PersonName,
                            CourseNumber = $"{existing.SubjCode} {existing.CrseNumb}",
                            Weeks = existing.Weeks
                        });
                    }
                }
                break;
        }

        return assignments;
    }

    private async Task<ClinicalImportResultDto> ExecuteImportInternalAsync(
        List<ClinicalSourceRecord> sourceData,
        List<ExistingClinicalRecord> existingRecords,
        int termCode,
        ClinicalImportMode mode,
        int modifiedBy,
        CancellationToken ct)
    {
        var result = new ClinicalImportResultDto { Success = true };

        // Aggregate source data by person+course (normalize keys to uppercase for case-insensitive matching)
        var sourceAggregated = sourceData
            .GroupBy(s => (MothraId: s.MothraId.ToUpperInvariant(), CourseKey: s.CourseKey.ToUpperInvariant()))
            .Select(g => new
            {
                g.Key.MothraId,
                g.Key.CourseKey,
                SubjCode = g.First().SubjCode,
                CrseNumb = g.First().CrseNumb,
                Weeks = g.Count(),
                FirstRecord = g.First()
            })
            .ToList();

        // Create lookup for existing records (normalize to uppercase)
        var existingLookup = existingRecords
            .GroupBy(e => (MothraId: e.MothraId.ToUpperInvariant(), CourseKey: BuildNormalizedCourseKey(e.SubjCode, e.CrseNumb)))
            .ToDictionary(g => g.Key, g => g.First());

        var sourceKeys = sourceAggregated
            .Select(s => (s.MothraId, s.CourseKey))
            .ToHashSet();

        switch (mode)
        {
            case ClinicalImportMode.AddNewOnly:
                foreach (var source in sourceAggregated)
                {
                    ct.ThrowIfCancellationRequested();

                    var key = (source.MothraId, source.CourseKey);
                    if (existingLookup.ContainsKey(key))
                    {
                        result.RecordsSkipped++;
                        continue;
                    }

                    var created = await CreateClinicalRecordAsync(source.MothraId, source.SubjCode, source.CrseNumb,
                        source.Weeks, termCode, modifiedBy, ct);
                    if (created)
                    {
                        result.RecordsAdded++;
                    }
                    else
                    {
                        result.RecordsSkipped++;
                    }
                }
                break;

            case ClinicalImportMode.ClearReplace:
                // Delete all existing clinical records
                var recordsToDelete = await _context.Records
                    .Where(r => r.TermCode == termCode)
                    .Where(r => r.EffortTypeId == ClinicalEffortTypes.Clinical)
                    .ToListAsync(ct);

                _context.Records.RemoveRange(recordsToDelete);
                result.RecordsDeleted = recordsToDelete.Count;

                // Persist deletes so CreateClinicalRecordAsync won't find stale records
                await _context.SaveChangesAsync(ct);

                // Add all source records
                foreach (var source in sourceAggregated)
                {
                    ct.ThrowIfCancellationRequested();

                    var created = await CreateClinicalRecordAsync(source.MothraId, source.SubjCode, source.CrseNumb,
                        source.Weeks, termCode, modifiedBy, ct);
                    if (created)
                    {
                        result.RecordsAdded++;
                    }
                    else
                    {
                        result.RecordsSkipped++;
                    }
                }
                break;

            case ClinicalImportMode.Sync:
                // Process source records - add new or update existing
                foreach (var source in sourceAggregated)
                {
                    ct.ThrowIfCancellationRequested();

                    var key = (source.MothraId, source.CourseKey);
                    if (existingLookup.TryGetValue(key, out var existing))
                    {
                        // Update if weeks changed
                        if (existing.Weeks != source.Weeks)
                        {
                            var record = await _context.Records.FindAsync([existing.Id], ct);
                            if (record != null)
                            {
                                record.Weeks = source.Weeks;
                                record.ModifiedDate = DateTime.Now;
                                record.ModifiedBy = modifiedBy;
                                result.RecordsUpdated++;
                            }
                        }
                        else
                        {
                            result.RecordsSkipped++;
                        }
                    }
                    else
                    {
                        var created = await CreateClinicalRecordAsync(source.MothraId, source.SubjCode, source.CrseNumb,
                            source.Weeks, termCode, modifiedBy, ct);
                        if (created)
                        {
                            result.RecordsAdded++;
                        }
                        else
                        {
                            result.RecordsSkipped++;
                        }
                    }
                }

                // Delete records not in source
                foreach (var existing in existingRecords)
                {
                    ct.ThrowIfCancellationRequested();

                    var key = (existing.MothraId.ToUpperInvariant(), CourseKey: BuildNormalizedCourseKey(existing.SubjCode, existing.CrseNumb));
                    if (!sourceKeys.Contains(key))
                    {
                        var record = await _context.Records.FindAsync([existing.Id], ct);
                        if (record != null)
                        {
                            _context.Records.Remove(record);
                            result.RecordsDeleted++;
                        }
                    }
                }
                break;
        }

        return result;
    }

    private async Task<ClinicalImportResultDto> ExecuteImportWithProgressInternalAsync(
        List<ClinicalSourceRecord> sourceData,
        List<ExistingClinicalRecord> existingRecords,
        int termCode,
        ClinicalImportMode mode,
        int modifiedBy,
        ChannelWriter<ClinicalImportProgressEvent> progressChannel,
        CancellationToken ct)
    {
        var result = new ClinicalImportResultDto { Success = true };

        // Aggregate source data by person+course (normalize keys to uppercase for case-insensitive matching)
        var sourceAggregated = sourceData
            .GroupBy(s => (MothraId: s.MothraId.ToUpperInvariant(), CourseKey: s.CourseKey.ToUpperInvariant()))
            .Select(g => new
            {
                g.Key.MothraId,
                g.Key.CourseKey,
                SubjCode = g.First().SubjCode,
                CrseNumb = g.First().CrseNumb,
                Weeks = g.Count(),
                FirstRecord = g.First()
            })
            .ToList();

        // Create lookup for existing records (normalize to uppercase)
        var existingLookup = existingRecords
            .GroupBy(e => (MothraId: e.MothraId.ToUpperInvariant(), CourseKey: BuildNormalizedCourseKey(e.SubjCode, e.CrseNumb)))
            .ToDictionary(g => g.Key, g => g.First());

        var sourceKeys = sourceAggregated
            .Select(s => (s.MothraId, s.CourseKey))
            .ToHashSet();

        var totalOperations = mode switch
        {
            ClinicalImportMode.AddNewOnly => sourceAggregated.Count,
            ClinicalImportMode.ClearReplace => existingRecords.Count + sourceAggregated.Count,
            ClinicalImportMode.Sync => sourceAggregated.Count + existingRecords.Count,
            _ => sourceAggregated.Count
        };

        var currentOperation = 0;

        switch (mode)
        {
            case ClinicalImportMode.AddNewOnly:
                foreach (var source in sourceAggregated)
                {
                    ct.ThrowIfCancellationRequested();

                    var key = (source.MothraId, source.CourseKey);
                    if (existingLookup.ContainsKey(key))
                    {
                        result.RecordsSkipped++;
                    }
                    else
                    {
                        var created = await CreateClinicalRecordAsync(source.MothraId, source.SubjCode, source.CrseNumb,
                            source.Weeks, termCode, modifiedBy, ct);
                        if (created)
                        {
                            result.RecordsAdded++;
                        }
                        else
                        {
                            result.RecordsSkipped++;
                        }
                    }

                    currentOperation++;
                    await progressChannel.WriteAsync(ClinicalImportProgressEvent.Importing(currentOperation, totalOperations), ct);
                }
                break;

            case ClinicalImportMode.ClearReplace:
                // Delete all existing clinical records
                var recordsToDelete = await _context.Records
                    .Where(r => r.TermCode == termCode)
                    .Where(r => r.EffortTypeId == ClinicalEffortTypes.Clinical)
                    .ToListAsync(ct);

                foreach (var record in recordsToDelete)
                {
                    ct.ThrowIfCancellationRequested();
                    _context.Records.Remove(record);
                    result.RecordsDeleted++;
                    currentOperation++;
                    await progressChannel.WriteAsync(ClinicalImportProgressEvent.Importing(currentOperation, totalOperations), ct);
                }

                // Persist deletes so CreateClinicalRecordAsync won't find stale records
                await _context.SaveChangesAsync(ct);

                // Add all source records
                foreach (var source in sourceAggregated)
                {
                    ct.ThrowIfCancellationRequested();

                    var created = await CreateClinicalRecordAsync(source.MothraId, source.SubjCode, source.CrseNumb,
                        source.Weeks, termCode, modifiedBy, ct);
                    if (created)
                    {
                        result.RecordsAdded++;
                    }
                    else
                    {
                        result.RecordsSkipped++;
                    }

                    currentOperation++;
                    await progressChannel.WriteAsync(ClinicalImportProgressEvent.Importing(currentOperation, totalOperations), ct);
                }
                break;

            case ClinicalImportMode.Sync:
                // Process source records - add new or update existing
                foreach (var source in sourceAggregated)
                {
                    ct.ThrowIfCancellationRequested();

                    var key = (source.MothraId, source.CourseKey);
                    if (existingLookup.TryGetValue(key, out var existing))
                    {
                        // Update if weeks changed
                        if (existing.Weeks != source.Weeks)
                        {
                            var record = await _context.Records.FindAsync([existing.Id], ct);
                            if (record != null)
                            {
                                record.Weeks = source.Weeks;
                                record.ModifiedDate = DateTime.Now;
                                record.ModifiedBy = modifiedBy;
                                result.RecordsUpdated++;
                            }
                        }
                        else
                        {
                            result.RecordsSkipped++;
                        }
                    }
                    else
                    {
                        var created = await CreateClinicalRecordAsync(source.MothraId, source.SubjCode, source.CrseNumb,
                            source.Weeks, termCode, modifiedBy, ct);
                        if (created)
                        {
                            result.RecordsAdded++;
                        }
                        else
                        {
                            result.RecordsSkipped++;
                        }
                    }

                    currentOperation++;
                    await progressChannel.WriteAsync(ClinicalImportProgressEvent.Importing(currentOperation, totalOperations), ct);
                }

                // Delete records not in source
                foreach (var existing in existingRecords)
                {
                    ct.ThrowIfCancellationRequested();

                    var key = (existing.MothraId.ToUpperInvariant(), CourseKey: BuildNormalizedCourseKey(existing.SubjCode, existing.CrseNumb));
                    if (!sourceKeys.Contains(key))
                    {
                        var record = await _context.Records.FindAsync([existing.Id], ct);
                        if (record != null)
                        {
                            _context.Records.Remove(record);
                            result.RecordsDeleted++;
                        }
                    }

                    currentOperation++;
                    await progressChannel.WriteAsync(ClinicalImportProgressEvent.Importing(currentOperation, totalOperations), ct);
                }
                break;
        }

        return result;
    }

    /// <summary>
    /// Create a clinical effort record for the given instructor and course.
    /// </summary>
    private async Task<bool> CreateClinicalRecordAsync(
        string mothraId,
        string subjCode,
        string crseNumb,
        int weeks,
        int termCode,
        int modifiedBy,
        CancellationToken ct)
    {
        // Look up person by MothraId
        var person = await _context.ViperPersons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MothraId == mothraId, ct);

        if (person == null)
        {
            _logger.LogWarning("Person not found for MothraId {MothraId} during clinical import", mothraId);
            return false;
        }

        // Check if person exists in effort system for this term
        var effortPerson = await _context.Persons
            .FirstOrDefaultAsync(p => p.PersonId == person.PersonId && p.TermCode == termCode, ct);

        if (effortPerson == null)
        {
            _logger.LogWarning("Effort person not found for PersonId {PersonId}, term {TermCode} during clinical import",
                person.PersonId, termCode);
            return false;
        }

        // Look up course
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.TermCode == termCode && c.SubjCode == subjCode && c.CrseNumb == crseNumb, ct);

        if (course == null)
        {
            _logger.LogWarning("Course not found for {SubjCode} {CrseNumb}, term {TermCode} during clinical import",
                subjCode, crseNumb, termCode);
            return false;
        }

        // Check if record already exists (composite key: CourseId + PersonId + EffortTypeId)
        var existingRecord = await _context.Records
            .FirstOrDefaultAsync(r =>
                r.CourseId == course.Id &&
                r.PersonId == person.PersonId &&
                r.EffortTypeId == EffortConstants.ClinicalEffortType, ct);

        if (existingRecord != null)
        {
            // Record already exists, update weeks
            existingRecord.Weeks = weeks;
            existingRecord.ModifiedDate = DateTime.Now;
            existingRecord.ModifiedBy = modifiedBy;
            return true;
        }

        // Create new record
        var record = new EffortRecord
        {
            CourseId = course.Id,
            PersonId = person.PersonId,
            TermCode = termCode,
            EffortTypeId = EffortConstants.ClinicalEffortType,
            RoleId = EffortConstants.ClinicalInstructorRoleId,
            Weeks = weeks,
            Crn = course.Crn,
            ModifiedDate = DateTime.Now,
            ModifiedBy = modifiedBy
        };

        _context.Records.Add(record);
        return true;
    }

    private static string GetPriorityCourse(List<string> courses)
    {
        if (courses.Count == 1)
        {
            return courses[0];
        }

        return courses.MinBy(c => ClinicalCoursePriority.GetValueOrDefault(c, 0)) ?? courses[0];
    }

    /// <summary>
    /// Creates a normalized course key for case-insensitive lookups (uppercase, trimmed whitespace).
    /// </summary>
    private static string BuildNormalizedCourseKey(string? subjCode, string? crseNumb) =>
        $"{subjCode?.Trim()} {crseNumb?.Trim()}".ToUpperInvariant();

    /// <summary>
    /// Internal record for source clinical schedule data.
    /// </summary>
    private sealed class ClinicalSourceRecord
    {
        public string MothraId { get; set; } = "";
        public int WeekId { get; set; }
        public DateTime WeekStart { get; set; }
        public string SubjCode { get; set; } = "";
        public string CrseNumb { get; set; } = "";
        public string CourseKey { get; set; } = "";
        public string RotationName { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string PersonName { get; set; } = "";
    }

    /// <summary>
    /// Internal record for existing clinical effort records.
    /// </summary>
    private sealed class ExistingClinicalRecord
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string MothraId { get; set; } = "";
        public int CourseId { get; set; }
        public string SubjCode { get; set; } = "";
        public string CrseNumb { get; set; } = "";
        public int Weeks { get; set; }
        public string PersonName { get; set; } = "";
    }
}
