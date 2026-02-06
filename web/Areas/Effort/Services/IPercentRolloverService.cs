using System.Threading.Channels;
using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for rolling over percent assignments to a new academic year during harvest.
/// </summary>
public interface IPercentRolloverService
{
    /// <summary>
    /// Check if this term requires percent assignment rollover (Fall terms only).
    /// </summary>
    bool ShouldRollover(int termCode);

    /// <summary>
    /// Generate preview of percent assignments that will be rolled over.
    /// </summary>
    Task<PercentRolloverPreviewDto> GetRolloverPreviewAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Execute the rollover - create new percentage records for the new academic year.
    /// Idempotent: skips records that already exist in the target year.
    /// Returns the count of records created.
    /// </summary>
    Task<int> ExecuteRolloverAsync(int termCode, int modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Execute rollover with real-time progress reporting via Channel.
    /// Used for SSE streaming to provide live progress updates to the client.
    /// </summary>
    /// <param name="termCode">The term code to rollover for.</param>
    /// <param name="modifiedBy">PersonId of the user performing the rollover.</param>
    /// <param name="progressChannel">Channel to write progress events to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of records created.</returns>
    Task<int> ExecuteRolloverWithProgressAsync(
        int termCode,
        int modifiedBy,
        ChannelWriter<RolloverProgressEvent> progressChannel,
        CancellationToken ct = default);
}
