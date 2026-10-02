using System.Text.Json;
using Viper.Areas.Reports.Engine;
using Viper.Areas.Reports.Exporters;
using Viper.Classes.Utilities;

namespace Viper.Areas.Reports.Services;

/// <summary>
/// A report the current user may run, as listed in a catalog. <c>Available</c> is
/// false for a planned report that is listed but not built yet.
/// </summary>
public sealed record ReportCatalogItem(string Key, string Title, string Area, string Description, bool Available);

/// <summary>
/// An exported report, ready to send as a download.
/// </summary>
public sealed record ReportFile(byte[] Content, string ContentType, string FileName);

public interface IReportService
{
    /// <summary>
    /// Reports the current user may run, built and planned, optionally limited to one area and
    /// ordered by area and then title.
    /// </summary>
    IReadOnlyList<ReportCatalogItem> GetCatalog(string? area);

    ReportOutcome<ReportDefinitionMetadata> GetDefinition(string? key);

    Task<ReportOutcome<ReportResult>> RunAsync(string? key, JsonElement? parameters, CancellationToken ct);

    Task<ReportOutcome<ReportFile>> ExportAsync(string? key, string? format, JsonElement? parameters, CancellationToken ct);
}

/// <summary>
/// Runs reports for the signed-in user: checks access before reading any parameters, binds and
/// validates them, runs the report, and writes an audit log entry for every run and export.
/// </summary>
public class ReportService : IReportService
{
    public const string FormatParameter = "format";

    private readonly IReportRegistry _registry;
    private readonly IReportUserService _userService;
    private readonly IReadOnlyList<IReportExporter> _exporters;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRegistry registry,
        IReportUserService userService,
        IEnumerable<IReportExporter> exporters,
        TimeProvider timeProvider,
        ILogger<ReportService> logger)
    {
        _registry = registry;
        _userService = userService;
        _exporters = [.. exporters];
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public IReadOnlyList<ReportCatalogItem> GetCatalog(string? area)
    {
        ReportContext context = _userService.GetCurrentContext();
        string? areaFilter = string.IsNullOrWhiteSpace(area) ? null : area.Trim();
        bool InArea(IReportIdentity report) =>
            areaFilter is null || string.Equals(report.Area, areaFilter, StringComparison.OrdinalIgnoreCase);

        IEnumerable<ReportCatalogItem> built = _registry.All
            .Where(InArea)
            .Where(definition => definition.CanRun(context))
            .Select(definition => ToCatalogItem(definition, available: true));
        IEnumerable<ReportCatalogItem> planned = _registry.Planned
            .Where(InArea)
            .Where(report => context.HasAnyPermission(report.Permissions))
            .Select(report => ToCatalogItem(report, available: false));

        return [.. built.Concat(planned)
            .OrderBy(item => item.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)];
    }

    public ReportOutcome<ReportDefinitionMetadata> GetDefinition(string? key)
    {
        ReportContext context = _userService.GetCurrentContext();
        ReportOutcomeStatus access = CheckAccess(key, context, out IReportDefinition? definition);
        return definition is not null
            ? ReportOutcome.Ok(definition.GetMetadata(context))
            : ReportOutcome.Failed<ReportDefinitionMetadata>(access, []);
    }

    public async Task<ReportOutcome<ReportResult>> RunAsync(string? key, JsonElement? parameters, CancellationToken ct)
    {
        ReportContext context = _userService.GetCurrentContext();
        ReportOutcomeStatus access = CheckAccess(key, context, out IReportDefinition? definition);
        if (definition is null)
        {
            return ReportOutcome.Failed<ReportResult>(access, []);
        }

        ReportOutcome<ReportRun> run = await ExecuteAsync(definition, context, parameters, "run", ct);
        return run.Value is { } completed
            ? ReportOutcome.Ok(completed.Result)
            : ReportOutcome.Failed<ReportResult>(run.Status, run.Errors);
    }

    public async Task<ReportOutcome<ReportFile>> ExportAsync(
        string? key, string? format, JsonElement? parameters, CancellationToken ct)
    {
        ReportContext context = _userService.GetCurrentContext();
        ReportOutcomeStatus access = CheckAccess(key, context, out IReportDefinition? definition);
        if (definition is null)
        {
            return ReportOutcome.Failed<ReportFile>(access, []);
        }

        IReportExporter? exporter = _exporters.FirstOrDefault(
            candidate => string.Equals(candidate.Format, format, StringComparison.OrdinalIgnoreCase));
        if (exporter is null)
        {
            string supported = string.Join(", ", _exporters.Select(candidate => candidate.Format));
            return ReportOutcome.Invalid<ReportFile>(
                [new ReportValidationError(FormatParameter, $"Choose an export format: {supported}.")]);
        }

        ReportOutcome<ReportRun> run = await ExecuteAsync(definition, context, parameters, "export:" + exporter.Format, ct);
        if (run.Value is not { } completed)
        {
            return ReportOutcome.Failed<ReportFile>(run.Status, run.Errors);
        }

        var export = new ReportExport(
            completed.Result, definition.DescribeParameters(completed.Parameters), definition.IsSensitive);
        string fileName = ExcelHelper.BuildExportFilename(
            new ExportFilenameOptions { ReportName = definition.Title, Extension = exporter.Extension });
        return ReportOutcome.Ok(new ReportFile(exporter.Export(export), exporter.ContentType, fileName));
    }

    /// <summary>
    /// Finds the report and checks the user may run it. Returns <see cref="ReportOutcomeStatus.Ok"/>
    /// with the definition, or the failure status with a null definition.
    /// </summary>
    private ReportOutcomeStatus CheckAccess(string? key, ReportContext context, out IReportDefinition? definition)
    {
        definition = null;
        IReportDefinition? found = _registry.Find(key);
        if (found is null)
        {
            return ReportOutcomeStatus.NotFound;
        }

        if (!found.CanRun(context))
        {
            return ReportOutcomeStatus.Forbidden;
        }

        definition = found;
        return ReportOutcomeStatus.Ok;
    }

    private static ReportCatalogItem ToCatalogItem(IReportIdentity report, bool available)
    {
        return new ReportCatalogItem(report.Key, report.Title, report.Area, report.Description, available);
    }

    private async Task<ReportOutcome<ReportRun>> ExecuteAsync(
        IReportDefinition definition, ReportContext context, JsonElement? parameters, string action, CancellationToken ct)
    {
        if (!ReportParameterBinder.TryBind(definition.ParametersType, parameters, out object? bound, out ReportValidationError? bindError))
        {
            return ReportOutcome.Invalid<ReportRun>([bindError]);
        }

        IReadOnlyList<ReportValidationError> errors = definition.PrepareParameters(bound);
        if (errors.Count > 0)
        {
            return ReportOutcome.Invalid<ReportRun>(errors);
        }

        ReportResult result = await definition.RunAsync(bound, context, _timeProvider.GetLocalNow(), ct);

        // Reports expose salary, demographic and visa data, so every run and export is audited.
        // The guard skips serializing the parameters when information logging is off.
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Report {ReportKey} {Action} by {LoginId} returned {RowCount} rows for parameters {Parameters}",
                definition.Key,
                action,
                LogSanitizer.SanitizeId(context.LoginId),
                result.RowCount,
                LogSanitizer.SanitizeString(JsonSerializer.Serialize(bound, definition.ParametersType, ReportParameterBinder.JsonOptions)));
        }

        return ReportOutcome.Ok(new ReportRun(bound, result));
    }

    private sealed record ReportRun(object Parameters, ReportResult Result);
}
