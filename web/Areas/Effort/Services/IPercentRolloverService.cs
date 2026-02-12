using System.Threading.Channels;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for rolling over percent assignments to a new academic year.
/// The boundary year (e.g., 2025) determines the rollover:
/// assignments ending June 30 of that year are copied to July 1 – June 30 of the next year.
/// </summary>
public interface IPercentRolloverService
{
    /// <summary>
    /// Generate preview of percent assignments that will be rolled over.
    /// </summary>
    /// <param name="year">Boundary year (e.g., 2025 = AY 2024-2025 → 2025-2026).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PercentRolloverPreviewDto> GetRolloverPreviewAsync(int year, CancellationToken ct = default);

    /// <summary>
    /// Execute the rollover - create new percentage records for the new academic year.
    /// Idempotent: skips records that already exist in the target year.
    /// Returns the count of records created.
    /// </summary>
    /// <param name="year">Boundary year (e.g., 2025 = AY 2024-2025 → 2025-2026).</param>
    /// <param name="modifiedBy">PersonId of the user performing the rollover.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<int> ExecuteRolloverAsync(int year, int modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Execute rollover with real-time progress reporting via Channel.
    /// Used for SSE streaming to provide live progress updates to the client.
    /// </summary>
    /// <param name="year">Boundary year (e.g., 2025 = AY 2024-2025 → 2025-2026).</param>
    /// <param name="modifiedBy">PersonId of the user performing the rollover.</param>
    /// <param name="progressChannel">Channel to write progress events to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of records created.</returns>
    Task<int> ExecuteRolloverWithProgressAsync(
        int year,
        int modifiedBy,
        ChannelWriter<RolloverProgressEvent> progressChannel,
        CancellationToken ct = default);
}
