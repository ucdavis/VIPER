using System.Threading.Channels;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for importing clinical effort data from the Clinical Scheduler system.
/// Supports multiple import modes: AddNewOnly, ClearReplace, and Sync.
/// </summary>
public interface IClinicalImportService
{
    /// <summary>
    /// Validate which MothraIds have valid AAUD data (ids + employees + valid effort title code)
    /// for the given term. Used by both harvest and standalone import previews to filter
    /// records that would be skipped during execution.
    /// </summary>
    Task<ImportValidationResult> ValidateImportableInstructorsAsync(
        IEnumerable<string> mothraIds, int termCode, CancellationToken ct = default);

    /// <summary>
    /// Get the set of existing clinical record keys for a term, using normalized
    /// MothraId:CourseKey matching. Shared with HarvestService for preview comparison (DRY).
    /// </summary>
    Task<HashSet<string>> GetExistingClinicalRecordKeysAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Generate a preview of clinical import data without modifying the database.
    /// </summary>
    /// <param name="termCode">The term code to import for.</param>
    /// <param name="mode">The import mode determining how existing records are handled.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview of what would be imported, updated, deleted, or skipped.</returns>
    Task<ClinicalImportPreviewDto> GetPreviewAsync(int termCode, ClinicalImportMode mode, CancellationToken ct = default);

    /// <summary>
    /// Execute clinical import without progress reporting.
    /// </summary>
    /// <param name="termCode">The term code to import for.</param>
    /// <param name="mode">The import mode determining how existing records are handled.</param>
    /// <param name="modifiedBy">PersonId of the user performing the import.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the import operation.</returns>
    Task<ClinicalImportResultDto> ExecuteImportAsync(int termCode, ClinicalImportMode mode, int modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Execute clinical import with real-time progress reporting via Channel.
    /// Used for SSE streaming to provide live progress updates to the client.
    /// </summary>
    /// <param name="termCode">The term code to import for.</param>
    /// <param name="mode">The import mode determining how existing records are handled.</param>
    /// <param name="modifiedBy">PersonId of the user performing the import.</param>
    /// <param name="progressChannel">Channel to write progress events to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the import operation.</returns>
    Task<ClinicalImportResultDto> ExecuteImportWithProgressAsync(
        int termCode,
        ClinicalImportMode mode,
        int modifiedBy,
        ChannelWriter<ClinicalImportProgressEvent> progressChannel,
        CancellationToken ct = default);
}
