using Microsoft.AspNetCore.Mvc;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.ReportBuilder.Controllers;

/// <summary>
/// Lists, describes, runs and downloads admin-built reports. Signing in is enough to reach the
/// controller; each report's own view permission is checked by the services before anything
/// the user sent is read.
/// </summary>
[Route("/api/report-builder")]
[Permission(Allow = "SVMSecure")]
public class ReportBuilderController : ApiController
{
    private readonly IReportViewerService _viewer;
    private readonly IReportRunService _runs;
    private readonly IReportExportService _exports;

    public ReportBuilderController(IReportViewerService viewer, IReportRunService runs, IReportExportService exports)
    {
        _viewer = viewer;
        _runs = runs;
        _exports = exports;
    }

    /// <summary>The reports the user may view.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReportCatalogItem>>> GetCatalog(CancellationToken ct = default)
    {
        return Ok(await _viewer.GetCatalogAsync(ct));
    }

    /// <summary>A report's form fields, columns and settings.</summary>
    [HttpGet("{slug:guid}")]
    public async Task<ActionResult<ReportViewerDefinition>> GetDefinition(string slug, CancellationToken ct = default)
    {
        ReportDefinitionOutcome outcome = await _viewer.GetDefinitionAsync(slug, ct);
        return outcome.Status == ReportRunStatus.Ok ? Ok(outcome.Definition) : Failure(outcome.Status, outcome.Errors);
    }

    /// <summary>One page of the report (or all of it, when it isn't paginated).</summary>
    [HttpPost("{slug:guid}/run")]
    public async Task<ActionResult<ReportRunResult>> Run(string slug, [FromBody] ReportRunRequest request, CancellationToken ct = default)
    {
        ReportRunOutcome outcome = await _runs.RunAsync(slug, request, ReportRunMode.Screen, ct);
        return outcome.Status == ReportRunStatus.Ok ? Ok(outcome.Result) : Failure(outcome.Status, outcome.Errors);
    }

    /// <summary>
    /// The report as a download in <paramref name="format"/> (xlsx or pdf), or 204 No Content
    /// when it has no rows.
    /// </summary>
    [HttpPost("{slug:guid}/export/{format}")]
    public async Task<ActionResult> Export(string slug, string format, [FromBody] ReportRunRequest request, CancellationToken ct = default)
    {
        ReportExportOutcome outcome = await _exports.ExportAsync(slug, format, request, ct);
        if (outcome.Status != ReportRunStatus.Ok)
        {
            return Failure(outcome.Status, outcome.Errors);
        }

        if (outcome.File is not { } file)
        {
            return NoContent();
        }

        // Reports can hold confidential data, so neither the browser nor a proxy may keep a copy.
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        return File(file.Content, file.ContentType, file.FileName);
    }

    private ActionResult Failure(ReportRunStatus status, IReadOnlyList<string> errors)
    {
        return status switch
        {
            ReportRunStatus.NotFound => NotFound("Report not found."),
            ReportRunStatus.Forbidden => ForbidApi("You do not have access to this report."),
            ReportRunStatus.Invalid => BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(StringComparer.Ordinal) { [string.Empty] = [.. errors] })
            {
                Title = "The report request is not valid.",
            }),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable, errors.Count > 0 ? string.Join(" ", errors) : "This report is unavailable."),
        };
    }
}
