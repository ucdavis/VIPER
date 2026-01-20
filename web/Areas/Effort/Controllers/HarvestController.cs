using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for harvesting instructor and course data into the Effort system.
/// </summary>
[Route("/api/effort/terms/{termCode:int}/harvest")]
[Permission(Allow = EffortPermissions.HarvestTerm)]
public class HarvestController : BaseEffortController
{
    private readonly IHarvestService _harvestService;
    private readonly ITermService _termService;
    private readonly IEffortPermissionService _permissionService;

    public HarvestController(
        IHarvestService harvestService,
        ITermService termService,
        IEffortPermissionService permissionService,
        ILogger<HarvestController> logger) : base(logger)
    {
        _harvestService = harvestService;
        _termService = termService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Generate a preview of harvest data without saving.
    /// </summary>
    /// <param name="termCode">The term code to preview harvest for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview of all data that would be imported.</returns>
    [HttpGet("preview")]
    public async Task<ActionResult<HarvestPreviewDto>> GetPreview(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var term = await _termService.GetTermAsync(termCode, ct);
        if (term == null)
        {
            _logger.LogWarning("Term not found for harvest preview: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        if (term.Status is not ("Created" or "Harvested"))
        {
            _logger.LogWarning("Invalid term status for harvest: {TermCode} is {Status}", termCode, term.Status);
            return BadRequest($"Term must be in Created or Harvested status to harvest. Current status: {term.Status}");
        }

        _logger.LogInformation("Generating harvest preview for term {TermCode}", termCode);
        var preview = await _harvestService.GeneratePreviewAsync(termCode, ct);

        return Ok(preview);
    }

    /// <summary>
    /// Execute harvest: clear existing data and import all phases.
    /// </summary>
    /// <param name="termCode">The term code to harvest.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the harvest operation.</returns>
    [HttpPost("commit")]
    public async Task<ActionResult<HarvestResultDto>> CommitHarvest(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var term = await _termService.GetTermAsync(termCode, ct);
        if (term == null)
        {
            _logger.LogWarning("Term not found for harvest commit: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        if (term.Status is not ("Created" or "Harvested"))
        {
            _logger.LogWarning("Invalid term status for harvest: {TermCode} is {Status}", termCode, term.Status);
            return BadRequest($"Term must be in Created or Harvested status to harvest. Current status: {term.Status}");
        }

        var modifiedBy = _permissionService.GetCurrentPersonId();

        _logger.LogInformation("Starting harvest for term {TermCode} by user {ModifiedBy}", termCode, modifiedBy);

        var result = await _harvestService.ExecuteHarvestAsync(termCode, modifiedBy, ct);

        if (result.Success)
        {
            _logger.LogInformation("Harvest completed for term {TermCode}: {Instructors} instructors, {Courses} courses, {Records} records",
                termCode, result.Summary.TotalInstructors, result.Summary.TotalCourses, result.Summary.TotalEffortRecords);
            return Ok(result);
        }

        _logger.LogWarning("Harvest failed for term {TermCode}: {Error}", termCode, LogSanitizer.SanitizeString(result.ErrorMessage));
        return BadRequest(result);
    }

    /// <summary>
    /// Execute harvest with real-time progress updates via Server-Sent Events (SSE).
    /// </summary>
    /// <param name="termCode">The term code to harvest.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("stream")]
    public async Task StreamHarvest(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        // Validate term before starting stream
        var term = await _termService.GetTermAsync(termCode, ct);
        if (term == null)
        {
            _logger.LogWarning("Term not found for harvest stream: {TermCode}", termCode);
            Response.StatusCode = 404;
            return;
        }

        if (term.Status is not ("Created" or "Harvested"))
        {
            _logger.LogWarning("Invalid term status for harvest: {TermCode} is {Status}", termCode, term.Status);
            Response.StatusCode = 400;
            return;
        }

        var modifiedBy = _permissionService.GetCurrentPersonId();

        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation("Starting SSE harvest stream for term {TermCode} by user {ModifiedBy}", termCode, modifiedBy);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Create a channel for progress events
        var channel = Channel.CreateUnbounded<HarvestProgressEvent>();

        // Start the harvest in a background task
        var harvestTask = Task.Run(async () =>
        {
            try
            {
                await _harvestService.ExecuteHarvestWithProgressAsync(termCode, modifiedBy, channel.Writer, ct);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Harvest cancelled for term {TermCode}", termCode);
                channel.Writer.Complete();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during harvest for term {TermCode}", termCode);
                await channel.Writer.WriteAsync(HarvestProgressEvent.Failed("An invalid operation occurred during harvest."), ct);
                channel.Writer.Complete();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during harvest for term {TermCode}", termCode);
                await channel.Writer.WriteAsync(HarvestProgressEvent.Failed("A database error occurred during harvest."), ct);
                channel.Writer.Complete();
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, ct);

        try
        {
            // Read from the channel and stream to client
            await foreach (var progressEvent in channel.Reader.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(progressEvent, jsonOptions);
                await Response.WriteAsync($"event: {progressEvent.Type}\n", ct);
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Harvest stream cancelled for term {TermCode}", termCode);
        }

        await harvestTask;
    }
}
