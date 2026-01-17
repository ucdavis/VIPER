using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Models.Entities;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Effort.Services.Harvest;

/// <summary>
/// Shared state container for harvest operations.
/// Passed between harvest phases to share data and track progress.
/// </summary>
public sealed class HarvestContext
{
    /// <summary>
    /// The term code being harvested.
    /// </summary>
    public int TermCode { get; init; }

    /// <summary>
    /// PersonId of the user performing the harvest.
    /// </summary>
    public int ModifiedBy { get; init; }

    /// <summary>
    /// Preview data accumulated across phases.
    /// </summary>
    public HarvestPreviewDto Preview { get; } = new();

    /// <summary>
    /// MothraIds that have been imported into effort.Persons.
    /// Used to track which instructors are available for effort records.
    /// </summary>
    public HashSet<string> ImportedMothraIds { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Lookup from course key to CourseId for imported courses.
    /// Keys are either "CRN:{crn}" or "{SubjCode}{CrseNumb}-{Units}".
    /// </summary>
    public Dictionary<string, int> CourseIdLookup { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Title code to description lookup from dictionary.dbo.dvtTitle.
    /// </summary>
    public Dictionary<string, string>? TitleLookup { get; set; }

    /// <summary>
    /// Department code to simple name lookup.
    /// </summary>
    public Dictionary<string, string>? DeptSimpleNameLookup { get; set; }

    /// <summary>
    /// Warnings accumulated during harvest.
    /// </summary>
    public List<HarvestWarning> Warnings { get; } = [];

    /// <summary>
    /// Records created during harvest, pending audit logging.
    /// </summary>
    public List<(EffortRecord Record, HarvestRecordPreview Preview)> CreatedRecords { get; } = [];

    #region Progress Tracking

    /// <summary>
    /// Callback to report progress during execution. Set by orchestrator for live updates.
    /// Parameters: (currentInstructors, totalInstructors, currentCourses, totalCourses, currentRecords, totalRecords, phaseName)
    /// </summary>
    public Func<int, int, int, int, int, int, string, Task>? ProgressCallback { get; set; }

    /// <summary>
    /// Total instructors to import (set by orchestrator before execution).
    /// </summary>
    public int TotalInstructors { get; set; }

    /// <summary>
    /// Total courses to import (set by orchestrator before execution).
    /// </summary>
    public int TotalCourses { get; set; }

    /// <summary>
    /// Total records to import (set by orchestrator before execution).
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// Instructors imported so far across all phases.
    /// </summary>
    public int InstructorsImported { get; set; }

    /// <summary>
    /// Courses imported so far across all phases.
    /// </summary>
    public int CoursesImported { get; set; }

    /// <summary>
    /// Records imported so far across all phases.
    /// </summary>
    public int RecordsImported { get; set; }

    /// <summary>
    /// Last reported total for throttling.
    /// </summary>
    private int _lastReportedTotal;

    /// <summary>
    /// Report progress if callback is set.
    /// Throttled to report every 10 items to avoid flooding the SSE channel.
    /// </summary>
    public async Task ReportProgressAsync(string phaseName)
    {
        if (ProgressCallback == null) return;

        var currentTotal = InstructorsImported + CoursesImported + RecordsImported;

        // Report every 10 items, or if this is the first item
        if (currentTotal - _lastReportedTotal >= 10 || currentTotal == 1)
        {
            _lastReportedTotal = currentTotal;
            await ProgressCallback(
                InstructorsImported, TotalInstructors,
                CoursesImported, TotalCourses,
                RecordsImported, TotalRecords,
                phaseName);
        }
    }

    /// <summary>
    /// Force a progress report (used at end of phases).
    /// </summary>
    public async Task ForceReportProgressAsync(string phaseName)
    {
        if (ProgressCallback == null) return;

        _lastReportedTotal = InstructorsImported + CoursesImported + RecordsImported;
        await ProgressCallback(
            InstructorsImported, TotalInstructors,
            CoursesImported, TotalCourses,
            RecordsImported, TotalRecords,
            phaseName);
    }

    #endregion

    #region Database Contexts (passed through, not owned)

    /// <summary>
    /// Effort database context.
    /// </summary>
    public required EffortDbContext EffortContext { get; init; }

    /// <summary>
    /// CREST database context.
    /// </summary>
    public required CrestContext CrestContext { get; init; }

    /// <summary>
    /// Courses database context.
    /// </summary>
    public required CoursesContext CoursesContext { get; init; }

    /// <summary>
    /// VIPER database context.
    /// </summary>
    public required VIPERContext ViperContext { get; init; }

    /// <summary>
    /// AAUD database context.
    /// </summary>
    public required AAUDContext AaudContext { get; init; }

    /// <summary>
    /// Dictionary database context.
    /// </summary>
    public required DictionaryContext DictionaryContext { get; init; }

    #endregion

    #region Services

    /// <summary>
    /// Audit service for logging harvest operations.
    /// </summary>
    public required IEffortAuditService AuditService { get; init; }

    /// <summary>
    /// Instructor service for department resolution and title lookups.
    /// </summary>
    public required IInstructorService InstructorService { get; init; }

    /// <summary>
    /// Term service for term name lookups.
    /// </summary>
    public required ITermService TermService { get; init; }

    /// <summary>
    /// Logger for diagnostic messages.
    /// </summary>
    public required ILogger Logger { get; init; }

    #endregion
}
