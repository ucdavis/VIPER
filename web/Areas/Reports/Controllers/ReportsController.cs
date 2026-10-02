using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Reports.Controllers;

/// <summary>
/// Lists, describes, runs and exports code-defined reports. Signing in is enough to reach the controller;
/// each report's own permissions are checked by <see cref="IReportService"/> before any
/// parameters are read.
/// </summary>
[Route("/api/reports")]
[Permission(Allow = "SVMSecure")]
public class ReportsController : ApiController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<ReportCatalogItem>> GetCatalog([FromQuery] string? area = null)
    {
        return Ok(_reportService.GetCatalog(area));
    }

    [HttpGet("{key}")]
    public ActionResult<ReportDefinitionMetadata> GetDefinition(string key)
    {
        return ToActionResult(_reportService.GetDefinition(key));
    }

    [HttpPost("{key}/run")]
    public async Task<ActionResult<ReportResult>> Run(
        string key,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement? parameters,
        CancellationToken ct = default)
    {
        return ToActionResult(await _reportService.RunAsync(key, parameters, ct));
    }

    /// <summary>
    /// Runs the report and returns it as a file download in <paramref name="format"/> (csv, xlsx or pdf).
    /// </summary>
    [HttpPost("{key}/export/{format}")]
    public async Task<ActionResult> Export(
        string key,
        string format,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] JsonElement? parameters,
        CancellationToken ct = default)
    {
        ReportOutcome<ReportFile> outcome = await _reportService.ExportAsync(key, format, parameters, ct);
        if (outcome.Value is not { } file)
        {
            return ToActionResult(outcome);
        }

        // Exports hold personnel data, so neither the browser nor a proxy may keep a copy.
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Groups validation errors by parameter name; errors that span several parameters use an
    /// empty key, the same convention ASP.NET uses for model-level errors.
    /// </summary>
    internal static ValidationProblemDetails ToProblemDetails(IReadOnlyList<ReportValidationError> errors)
    {
        Dictionary<string, string[]> byParameter = errors
            .GroupBy(error => error.Parameter ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray(), StringComparer.Ordinal);
        return new ValidationProblemDetails(byParameter) { Title = "The report parameters are not valid." };
    }

    private ActionResult ToActionResult<T>(ReportOutcome<T> outcome)
    {
        return outcome.Status switch
        {
            ReportOutcomeStatus.Ok => Ok(outcome.Value),
            ReportOutcomeStatus.NotFound => NotFound("Report not found."),
            ReportOutcomeStatus.Forbidden => ForbidApi("You do not have access to this report."),
            _ => BadRequest(ToProblemDetails(outcome.Errors)),
        };
    }
}
