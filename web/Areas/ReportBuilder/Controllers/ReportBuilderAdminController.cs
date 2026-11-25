using Microsoft.AspNetCore.Mvc;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.ReportBuilder.Controllers;

/// <summary>
/// The Report Builder's admin API: the [report] sources, RAPS permissions to choose from,
/// creating, editing, deleting, restoring and previewing reports, and the reports that are
/// failing. Every action needs the Report Builder admin permission.
/// </summary>
[Route("/api/report-builder/admin")]
[Permission(Allow = ReportBuilderPermissions.Admin)]
public class ReportBuilderAdminController : ApiController
{
    private const string NotFoundMessage = "Report not found.";

    private readonly IReportAdminService _admin;
    private readonly IReportSourceCatalogService _catalog;
    private readonly IReportPermissionLookupService _permissions;
    private readonly IReportProblemService _problems;

    public ReportBuilderAdminController(
        IReportAdminService admin,
        IReportSourceCatalogService catalog,
        IReportPermissionLookupService permissions,
        IReportProblemService problems)
    {
        _admin = admin;
        _catalog = catalog;
        _permissions = permissions;
        _problems = problems;
    }

    /// <summary>The views and procedures in the [report] schema.</summary>
    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<ReportSourceSummary>>> GetSources(CancellationToken ct = default)
    {
        return Ok(await _catalog.GetSourcesAsync(ct));
    }

    /// <summary>A source's columns and, for a procedure, its parameters.</summary>
    [HttpGet("sources/{name}")]
    public async Task<ActionResult<ReportSourceDetail>> GetSource(string name, CancellationToken ct = default)
    {
        ReportSourceDetail? source = await _catalog.GetSourceAsync(name, ct);
        return source is null ? NotFound("Source not found.") : Ok(source);
    }

    /// <summary>RAPS permissions whose name contains the text.</summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<IReadOnlyList<ReportPermissionOption>>> SearchPermissions(
        [FromQuery] string? search = null, CancellationToken ct = default)
    {
        return Ok(await _permissions.SearchAsync(search, ct));
    }

    [HttpGet("definitions")]
    public async Task<ActionResult<IReadOnlyList<ReportAdminListItem>>> List(
        [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        return Ok(await _admin.ListAsync(includeDeleted, ct));
    }

    [HttpGet("definitions/{id:int}")]
    public async Task<ActionResult<ReportAdminDefinition>> Get(int id, CancellationToken ct = default)
    {
        ReportAdminDefinition? report = await _admin.GetAsync(id, ct);
        return report is null ? NotFound(NotFoundMessage) : Ok(report);
    }

    [HttpPost("definitions")]
    public async Task<ActionResult<ReportAdminDefinition>> Create([FromBody] ReportEditRequest request, CancellationToken ct = default)
    {
        ReportAdminOutcome<ReportAdminDefinition> outcome = await _admin.CreateAsync(request, ct);
        return outcome.Value is { } report
            ? CreatedAtAction(nameof(Get), new { id = report.Id }, report)
            : ToActionResult(outcome);
    }

    /// <summary>Saves the report; 409 when someone else saved it after the editor loaded it.</summary>
    [HttpPut("definitions/{id:int}")]
    public async Task<ActionResult<ReportAdminDefinition>> Update(int id, [FromBody] ReportEditRequest request, CancellationToken ct = default)
    {
        return ToActionResult(await _admin.UpdateAsync(id, request, ct));
    }

    /// <summary>Hides the report from viewers; it can be restored.</summary>
    [HttpDelete("definitions/{id:int}")]
    public async Task<ActionResult<ReportAdminDefinition>> Delete(int id, CancellationToken ct = default)
    {
        return ToActionResult(await _admin.DeleteAsync(id, ct));
    }

    [HttpPost("definitions/{id:int}/restore")]
    public async Task<ActionResult<ReportAdminDefinition>> Restore(int id, CancellationToken ct = default)
    {
        return ToActionResult(await _admin.RestoreAsync(id, ct));
    }

    [HttpGet("definitions/{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<ReportHistoryItem>>> GetHistory(int id, CancellationToken ct = default)
    {
        IReadOnlyList<ReportHistoryItem>? history = await _admin.GetHistoryAsync(id, ct);
        return history is null ? NotFound(NotFoundMessage) : Ok(history);
    }

    [HttpGet("definitions/{id:int}/history/{historyId:int}")]
    public async Task<ActionResult<ReportHistoryVersion>> GetHistoryVersion(int id, int historyId, CancellationToken ct = default)
    {
        ReportHistoryVersion? version = await _admin.GetHistoryVersionAsync(id, historyId, ct);
        return version is null ? NotFound("Version not found.") : Ok(version);
    }

    /// <summary>A page of an unsaved report, so the form can show what it will look like.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<ReportRunResult>> Preview([FromBody] ReportPreviewRequest request, CancellationToken ct = default)
    {
        ReportRunOutcome outcome = await _admin.PreviewAsync(request, ct);
        return outcome.Status switch
        {
            ReportRunStatus.Ok => Ok(outcome.Result),
            ReportRunStatus.Forbidden => ForbidApi("Only Report Builder admins can preview reports."),
            ReportRunStatus.Invalid => BadRequest(Invalid(outcome.Errors, "The report isn't ready to preview.")),
            _ => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                outcome.Errors.Count > 0 ? string.Join(" ", outcome.Errors) : "The preview couldn't be run."),
        };
    }

    /// <summary>Runs that failed in the last <paramref name="days"/> days, newest first.</summary>
    [HttpGet("failures")]
    public async Task<ActionResult<IReadOnlyList<ReportFailure>>> GetFailures([FromQuery] int? days = null, CancellationToken ct = default)
    {
        return Ok(await _problems.GetFailuresAsync(days, ct));
    }

    /// <summary>Every live report checked against the [report] catalog and RAPS, as a save would.</summary>
    [HttpGet("check")]
    public async Task<ActionResult<ReportCheckResult>> Check(CancellationToken ct = default)
    {
        return Ok(await _problems.CheckAsync(ct));
    }

    private ActionResult ToActionResult(ReportAdminOutcome<ReportAdminDefinition> outcome)
    {
        return outcome.Status switch
        {
            ReportAdminStatus.Ok => Ok(outcome.Value),
            ReportAdminStatus.NotFound => NotFound(NotFoundMessage),
            ReportAdminStatus.Conflict => Conflict(string.Join(" ", outcome.Errors)),
            _ => BadRequest(Invalid(outcome.Errors, "The report can't be saved.")),
        };
    }

    private static ValidationProblemDetails Invalid(IReadOnlyList<string> errors, string title)
    {
        return new ValidationProblemDetails(new Dictionary<string, string[]>(StringComparer.Ordinal) { [string.Empty] = [.. errors] })
        {
            Title = title,
        };
    }
}
