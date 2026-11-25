using System.Globalization;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>The choices offered by a report's choice parameters.</summary>
public interface IReportChoiceService
{
    /// <summary>
    /// Each choice parameter's choices, by parameter name (ignoring case). A parameter whose
    /// list can't be read has no entry, so the binder refuses any value for it.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>>> GetChoicesAsync(
        IEnumerable<ReportParameter> parameters, CancellationToken ct);
}

/// <summary>
/// Reads choices from the parameter's fixed list (<c>choicesList</c>, JSON) or, when it has
/// none, from a [report] view (<c>choicesSource</c>) with <c>Value</c> and <c>Label</c> columns.
/// </summary>
public sealed class ReportChoiceService : IReportChoiceService
{
    private readonly IReportSourceCatalogService _catalog;
    private readonly IReportQueryExecutor _executor;

    public ReportChoiceService(IReportSourceCatalogService catalog, IReportQueryExecutor executor)
    {
        _catalog = catalog;
        _executor = executor;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>>> GetChoicesAsync(
        IEnumerable<ReportParameter> parameters, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var choices = new Dictionary<string, IReadOnlyList<ReportChoice>>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<ReportSourceSummary>? sources = null;

        foreach (ReportParameter parameter in parameters.Where(IsChoice))
        {
            IReadOnlyList<ReportChoice>? list = ReportChoiceList.Parse(parameter.ChoicesList);
            if (list is null && !string.IsNullOrWhiteSpace(parameter.ChoicesSource))
            {
                sources ??= await _catalog.GetSourcesAsync(ct);
                list = await FromViewAsync(sources, parameter.ChoicesSource.Trim(), ct);
            }

            if (list is not null)
            {
                choices[parameter.Name!] = [.. list.Take(ReportLimits.MaxChoices)];
            }
        }

        return choices;
    }

    private static bool IsChoice(ReportParameter parameter)
    {
        return parameter.Type is ReportParameterType.Choice or ReportParameterType.MultiChoice
            && !string.IsNullOrWhiteSpace(parameter.Name);
    }

    private async Task<IReadOnlyList<ReportChoice>?> FromViewAsync(
        IReadOnlyList<ReportSourceSummary> sources, string name, CancellationToken ct)
    {
        // Only a view the catalog lists, so a choice list can't read anything outside [report].
        ReportSourceSummary? view = sources.FirstOrDefault(source =>
            source.Kind == ReportSourceKind.View && string.Equals(source.Name, name, StringComparison.OrdinalIgnoreCase));
        if (view is null)
        {
            return null;
        }

        ReportQueryResult result = await _executor.QueryAsync(
            ReportSqlBuilder.BuildChoices(view.Name), ReportLimits.MaxChoices, ReportLimits.ScreenTimeout, ct);
        return [.. result.Rows.Select(ToChoice).OfType<ReportChoice>()];
    }

    private static ReportChoice? ToChoice(IReadOnlyDictionary<string, object?> row)
    {
        string? value = Text(row, "Value");
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        string? label = Text(row, "Label");
        return new ReportChoice { Value = value, Label = string.IsNullOrWhiteSpace(label) ? value : label };
    }

    private static string? Text(IReadOnlyDictionary<string, object?> row, string column)
    {
        return row.TryGetValue(column, out object? value) && value is not null
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;
    }
}
