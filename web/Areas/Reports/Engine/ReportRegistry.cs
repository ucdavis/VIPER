namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Every report registered with the application, looked up by key.
/// </summary>
public interface IReportRegistry
{
    /// <summary>
    /// All reports, ordered by area and then title.
    /// </summary>
    IReadOnlyList<IReportDefinition> All { get; }

    /// <summary>
    /// Reports listed in catalogs but not built yet, ordered by area and then title.
    /// </summary>
    IReadOnlyList<PlannedReport> Planned { get; }

    /// <summary>
    /// The built report with this key, or null. Planned reports are never returned.
    /// </summary>
    IReportDefinition? Find(string? key);
}

/// <summary>
/// Validates report identities when the registry is created, so a duplicate or malformed key
/// fails on the first request instead of producing an unreachable report.
/// </summary>
public sealed class ReportRegistry : IReportRegistry
{
    private readonly Dictionary<string, IReportDefinition> _byKey = new(StringComparer.OrdinalIgnoreCase);

    public ReportRegistry(IEnumerable<IReportDefinition> definitions, IEnumerable<IPlannedReportSource>? plannedSources = null)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        foreach (IReportDefinition definition in definitions)
        {
            EnsureValidIdentity(definition);
            if (!_byKey.TryAdd(definition.Key, definition))
            {
                throw DuplicateKey(definition.Key);
            }
        }

        var plannedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<PlannedReport> planned = [.. (plannedSources ?? []).SelectMany(source => source.PlannedReports)];
        foreach (PlannedReport report in planned)
        {
            EnsureValidIdentity(report);
            if (_byKey.ContainsKey(report.Key) || !plannedKeys.Add(report.Key))
            {
                throw DuplicateKey(report.Key);
            }
        }

        All = [.. InCatalogOrder(_byKey.Values)];
        Planned = [.. InCatalogOrder(planned)];
    }

    public IReadOnlyList<IReportDefinition> All { get; }

    public IReadOnlyList<PlannedReport> Planned { get; }

    public IReportDefinition? Find(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? null : _byKey.GetValueOrDefault(key);
    }

    private static IEnumerable<T> InCatalogOrder<T>(IEnumerable<T> reports) where T : IReportIdentity
    {
        return reports
            .OrderBy(report => report.Area, StringComparer.OrdinalIgnoreCase)
            .ThenBy(report => report.Title, StringComparer.OrdinalIgnoreCase);
    }

    private static InvalidOperationException DuplicateKey(string key)
    {
        return new InvalidOperationException(
            $"More than one report uses the key '{key}'. Remove a planned report once its definition exists.");
    }

    private static void EnsureValidIdentity(IReportIdentity report)
    {
        if (!ReportKey.IsValid(report.Key))
        {
            throw new InvalidOperationException(
                $"Report key '{report.Key}' must look like 'area.report-name' (lowercase letters, digits and hyphens).");
        }

        if (string.IsNullOrWhiteSpace(report.Title)
            || string.IsNullOrWhiteSpace(report.Area)
            || string.IsNullOrWhiteSpace(report.Description))
        {
            throw new InvalidOperationException($"Report '{report.Key}' needs a title, an area and a description.");
        }

        if (report.Permissions.Count == 0 || report.Permissions.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"Report '{report.Key}' needs at least one non-blank permission.");
        }
    }
}
