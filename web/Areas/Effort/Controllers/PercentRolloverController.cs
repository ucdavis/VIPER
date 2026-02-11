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
/// API controller for standalone percent assignment rollover.
/// The year parameter is the boundary year (e.g., 2025 = AY 2024-2025 → 2025-2026).
/// </summary>
[Route("/api/effort/rollover/{year:int}")]
[Permission(Allow = EffortPermissions.ManageTerms)]
public class PercentRolloverController : BaseEffortController
{
    private readonly IPercentRolloverService _rolloverService;
    private readonly IEffortPermissionService _permissionService;

    public PercentRolloverController(
        IPercentRolloverService rolloverService,
        IEffortPermissionService permissionService,
        ILogger<PercentRolloverController> logger) : base(logger)
    {
        _rolloverService = rolloverService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Generate a preview of percent assignment rollover data without saving.
    /// </summary>
    /// <param name="year">The boundary year to preview rollover for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview of assignments that would be rolled over.</returns>
    [HttpGet("preview")]
    public async Task<ActionResult<PercentRolloverPreviewDto>> GetPreview(int year, CancellationToken ct)
    {
        SetExceptionContext("year", year);

        if (year < 2020 || year > DateTime.Now.Year)
        {
            _logger.LogWarning("Invalid year for rollover preview: {Year}", LogSanitizer.SanitizeYear(year));
            return BadRequest($"Year must be between 2020 and {DateTime.Now.Year}");
        }

        _logger.LogInformation("Generating rollover preview for year {Year}", LogSanitizer.SanitizeYear(year));
        var preview = await _rolloverService.GetRolloverPreviewAsync(year, ct);

        return Ok(preview);
    }

    /// <summary>
    /// Execute percent assignment rollover with real-time progress updates via Server-Sent Events (SSE).
    /// </summary>
    /// <param name="year">The boundary year to rollover for.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("stream")]
    public async Task StreamRollover(int year, CancellationToken ct)
    {
        SetExceptionContext("year", year);

        // CSRF protection: validate same-origin request
        if (!ValidateSameOrigin())
        {
            Response.StatusCode = 403;
            return;
        }

        if (year < 2020 || year > DateTime.Now.Year)
        {
            _logger.LogWarning("Invalid year for rollover stream: {Year}", LogSanitizer.SanitizeYear(year));
            Response.StatusCode = 400;
            return;
        }

        var modifiedBy = _permissionService.GetCurrentPersonId();

        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation("Starting SSE rollover stream for year {Year} by user {ModifiedBy}", LogSanitizer.SanitizeYear(year), modifiedBy);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Create a channel for progress events
        var channel = Channel.CreateUnbounded<RolloverProgressEvent>();

        // Start the rollover in a background task
        var rolloverTask = Task.Run(async () =>
        {
            try
            {
                await _rolloverService.ExecuteRolloverWithProgressAsync(year, modifiedBy, channel.Writer, ct);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Rollover cancelled for year {Year}", LogSanitizer.SanitizeYear(year));
                channel.Writer.Complete();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during rollover for year {Year}", LogSanitizer.SanitizeYear(year));
                await channel.Writer.WriteAsync(RolloverProgressEvent.Failed("An invalid operation occurred during rollover."), ct);
                channel.Writer.Complete();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during rollover for year {Year}", LogSanitizer.SanitizeYear(year));
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
            _logger.LogInformation(ex, "Rollover stream cancelled for year {Year}", LogSanitizer.SanitizeYear(year));
        }

        await rolloverTask;
    }
}
