using Viper.Areas.Effort.Models.DTOs.Responses;

namespace Viper.Areas.Effort.Services;

/// <summary>
/// Service for harvesting instructor and course data into the effort system.
/// </summary>
public interface IHarvestService
{
    /// <summary>
    /// Generate a preview of harvest data without saving.
    /// </summary>
    /// <param name="termCode">The term code to harvest.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview of all data that would be imported.</returns>
    Task<HarvestPreviewDto> GeneratePreviewAsync(int termCode, CancellationToken ct = default);

    /// <summary>
    /// Execute harvest: clear existing data and import all phases.
    /// </summary>
    /// <param name="termCode">The term code to harvest.</param>
    /// <param name="modifiedBy">PersonId of the user performing the harvest.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the harvest operation.</returns>
    Task<HarvestResultDto> ExecuteHarvestAsync(int termCode, int modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Execute harvest with real-time progress reporting via Channel.
    /// Used for SSE streaming to provide live progress updates to the client.
    /// </summary>
    /// <param name="termCode">The term code to harvest.</param>
    /// <param name="modifiedBy">PersonId of the user performing the harvest.</param>
    /// <param name="progressChannel">Channel to write progress events to.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteHarvestWithProgressAsync(
        int termCode,
        int modifiedBy,
        System.Threading.Channels.ChannelWriter<HarvestProgressEvent> progressChannel,
        CancellationToken ct = default);
}
