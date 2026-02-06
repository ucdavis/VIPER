using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Helpers;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for standalone percent assignment rollover.
/// </summary>
[Route("/api/effort/terms/{termCode:int}/rollover")]
[Permission(Allow = EffortPermissions.ManageTerms)]
public class PercentRolloverController : BaseEffortController
{
    private readonly IPercentRolloverService _rolloverService;
    private readonly ITermService _termService;
    private readonly IEffortPermissionService _permissionService;

    public PercentRolloverController(
        IPercentRolloverService rolloverService,
        ITermService termService,
        IEffortPermissionService permissionService,
        ILogger<PercentRolloverController> logger) : base(logger)
    {
        _rolloverService = rolloverService;
        _termService = termService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Generate a preview of percent assignment rollover data without saving.
    /// </summary>
    /// <param name="termCode">The term code to preview rollover for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview of assignments that would be rolled over.</returns>
    [HttpGet("preview")]
    public async Task<ActionResult<PercentRolloverPreviewDto>> GetPreview(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var term = await _termService.GetTermAsync(termCode, ct);
        if (term == null)
        {
            _logger.LogWarning("Term not found for rollover preview: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        // Validate term eligibility (Fall term + status)
        if (!TermValidationHelper.CanRolloverPercent(term.Status, termCode))
        {
            if (!TermValidationHelper.IsFallTermByCode(termCode))
            {
                _logger.LogWarning("Invalid term for rollover: {TermCode} is not a Fall term", termCode);
                return BadRequest("Percent rollover is only available for Fall terms");
            }
            _logger.LogWarning("Invalid term status for rollover: {TermCode} (status: {Status})", termCode, term.Status);
            return BadRequest($"Percent rollover is not available for terms with status '{term.Status}'");
        }

        _logger.LogInformation("Generating rollover preview for term {TermCode}", termCode);
        var preview = await _rolloverService.GetRolloverPreviewAsync(termCode, ct);

        return Ok(preview);
    }

    /// <summary>
    /// Execute percent assignment rollover with real-time progress updates via Server-Sent Events (SSE).
    /// </summary>
    /// <param name="termCode">The term code to rollover for.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("stream")]
    public async Task StreamRollover(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        // CSRF protection: validate same-origin request
        if (!ValidateSameOrigin())
        {
            Response.StatusCode = 403;
            return;
        }

        // Validate term before starting stream
        var term = await _termService.GetTermAsync(termCode, ct);
        if (term == null)
        {
            _logger.LogWarning("Term not found for rollover stream: {TermCode}", termCode);
            Response.StatusCode = 404;
            return;
        }

        // Validate term eligibility (Fall term + status)
        if (!TermValidationHelper.CanRolloverPercent(term.Status, termCode))
        {
            if (!TermValidationHelper.IsFallTermByCode(termCode))
            {
                _logger.LogWarning("Invalid term for rollover: {TermCode} is not a Fall term", termCode);
            }
            else
            {
                _logger.LogWarning("Invalid term status for rollover: {TermCode} (status: {Status})", termCode, term.Status);
            }
            Response.StatusCode = 400;
            return;
        }

        var modifiedBy = _permissionService.GetCurrentPersonId();

        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation("Starting SSE rollover stream for term {TermCode} by user {ModifiedBy}", termCode, modifiedBy);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Create a channel for progress events
        var channel = Channel.CreateUnbounded<RolloverProgressEvent>();

        // Start the rollover in a background task
        var rolloverTask = Task.Run(async () =>
        {
            try
            {
                await _rolloverService.ExecuteRolloverWithProgressAsync(termCode, modifiedBy, channel.Writer, ct);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Rollover cancelled for term {TermCode}", termCode);
                channel.Writer.Complete();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during rollover for term {TermCode}", termCode);
                await channel.Writer.WriteAsync(RolloverProgressEvent.Failed("An invalid operation occurred during rollover."), ct);
                channel.Writer.Complete();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during rollover for term {TermCode}", termCode);
                await channel.Writer.WriteAsync(RolloverProgressEvent.Failed("A database error occurred during rollover."), ct);
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
            _logger.LogInformation(ex, "Rollover stream cancelled for term {TermCode}", termCode);
        }

        await rolloverTask;
    }
}
