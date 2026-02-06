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
/// API controller for standalone clinical data import.
/// </summary>
[Route("/api/effort/terms/{termCode:int}/clinical")]
[Permission(Allow = EffortPermissions.ManageTerms)]
public class ClinicalImportController : BaseEffortController
{
    private readonly IClinicalImportService _importService;
    private readonly ITermService _termService;
    private readonly IEffortPermissionService _permissionService;

    public ClinicalImportController(
        IClinicalImportService importService,
        ITermService termService,
        IEffortPermissionService permissionService,
        ILogger<ClinicalImportController> logger) : base(logger)
    {
        _importService = importService;
        _termService = termService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Generate a preview of clinical import data without saving.
    /// </summary>
    /// <param name="termCode">The term code to preview import for.</param>
    /// <param name="mode">The import mode (AddNewOnly, ClearReplace, Sync). Defaults to AddNewOnly.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Preview of clinical assignments that would be imported.</returns>
    [HttpGet("preview")]
    public async Task<ActionResult<ClinicalImportPreviewDto>> GetPreview(
        int termCode,
        [FromQuery] ClinicalImportMode mode = ClinicalImportMode.AddNewOnly,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        var term = await _termService.GetTermAsync(termCode, ct);
        if (term == null)
        {
            _logger.LogWarning("Term not found for clinical import preview: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        // Validate term eligibility
        if (!TermValidationHelper.CanImportClinical(term.Status, termCode))
        {
            _logger.LogWarning(
                "Invalid term for clinical import: {TermCode} (status: {Status}, semester: {IsSemester})",
                termCode, term.Status, TermValidationHelper.IsSemesterTerm(termCode));

            if (!TermValidationHelper.IsSemesterTerm(termCode))
            {
                return BadRequest("Clinical import is only available for semester terms (not quarter terms)");
            }

            return BadRequest($"Clinical import is not available for terms with status '{term.Status}'");
        }

        _logger.LogInformation("Generating clinical import preview for term {TermCode} with mode {Mode}", termCode, mode);
        var preview = await _importService.GetPreviewAsync(termCode, mode, ct);

        return Ok(preview);
    }

    /// <summary>
    /// Execute clinical import with real-time progress updates via Server-Sent Events (SSE).
    /// </summary>
    /// <param name="termCode">The term code to import for.</param>
    /// <param name="mode">The import mode (AddNewOnly, ClearReplace, Sync). Defaults to AddNewOnly.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("stream")]
    public async Task StreamImport(
        int termCode,
        [FromQuery] ClinicalImportMode mode = ClinicalImportMode.AddNewOnly,
        CancellationToken ct = default)
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
            _logger.LogWarning("Term not found for clinical import stream: {TermCode}", termCode);
            Response.StatusCode = 404;
            return;
        }

        // Validate term eligibility
        if (!TermValidationHelper.CanImportClinical(term.Status, termCode))
        {
            _logger.LogWarning(
                "Invalid term for clinical import: {TermCode} (status: {Status}, semester: {IsSemester})",
                termCode, term.Status, TermValidationHelper.IsSemesterTerm(termCode));
            Response.StatusCode = 400;
            return;
        }

        var modifiedBy = _permissionService.GetCurrentPersonId();

        // Set SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation(
            "Starting SSE clinical import stream for term {TermCode} with mode {Mode} by user {ModifiedBy}",
            termCode, mode, modifiedBy);

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Create a channel for progress events
        var channel = Channel.CreateUnbounded<ClinicalImportProgressEvent>();

        // Start the import in a background task
        var importTask = Task.Run(async () =>
        {
            try
            {
                await _importService.ExecuteImportWithProgressAsync(termCode, mode, modifiedBy, channel.Writer, ct);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Clinical import cancelled for term {TermCode}", termCode);
                channel.Writer.Complete();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation during clinical import for term {TermCode}", termCode);
                await channel.Writer.WriteAsync(ClinicalImportProgressEvent.Failed("An invalid operation occurred during import."), ct);
                channel.Writer.Complete();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during clinical import for term {TermCode}", termCode);
                await channel.Writer.WriteAsync(ClinicalImportProgressEvent.Failed("A database error occurred during import."), ct);
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
            _logger.LogInformation(ex, "Clinical import stream cancelled for term {TermCode}", termCode);
        }

        await importTask;
    }
}
