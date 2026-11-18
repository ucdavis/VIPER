using System.Globalization;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>A report's fields checked against the catalog and RAPS: what's wrong, or the names to save.</summary>
public sealed record ReportValidation
{
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>The source as the catalog describes it; null when it isn't in [report].</summary>
    public ReportSourceDetail? Source { get; init; }

    /// <summary>The view permissions as RAPS spells them, without repeats.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>Each name the catalog knows, as it spells it, so saved names match the source exactly.</summary>
    public IReadOnlyDictionary<string, string> SourceNames { get; init; } = new Dictionary<string, string>();
}

/// <summary>Checks a report before it's saved or previewed.</summary>
public interface IReportDefinitionValidator
{
    Task<ReportValidation> ValidateAsync(ReportEditRequest request, CancellationToken ct);
}

/// <summary>
/// Checks a report the way the runner will use it: the source is a [report] view or procedure,
/// every column and parameter exists in it, parameters' types, operators, choices and defaults
/// fit together, and every view permission is one RAPS has.
/// </summary>
public sealed class ReportDefinitionValidator : IReportDefinitionValidator
{
    private const int TextLength = 500;
    private const int PermissionLength = 100;
    private const int MaxEcho = 50;
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IReportSourceCatalogService _catalog;
    private readonly IReportPermissionLookupService _permissions;

    public ReportDefinitionValidator(IReportSourceCatalogService catalog, IReportPermissionLookupService permissions)
    {
        _catalog = catalog;
        _permissions = permissions;
    }

    public async Task<ReportValidation> ValidateAsync(ReportEditRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<string>();
        CheckReportFields(request, errors);

        ReportSourceDetail? source = string.IsNullOrWhiteSpace(request.Source)
            ? null
            : await _catalog.GetSourceAsync(request.Source.Trim(), ct);
        if (source is null)
        {
            errors.Add(string.IsNullOrWhiteSpace(request.Source)
                ? "Choose a data source."
                : $"'{Echo(request.Source)}' isn't a view or procedure in the [report] schema.");
        }

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (source is not null)
        {
            foreach (string name in source.Columns.Select(c => c.Name).Concat(source.Parameters.Select(p => p.Name)))
            {
                names.TryAdd(name, name);
            }

            CheckColumns(request, source, errors);
            await CheckParametersAsync(request, source, errors, ct);
        }

        IReadOnlyList<string> permissions = await CheckPermissionsAsync(request.Permissions, errors, ct);
        return new ReportValidation { Errors = errors, Source = source, Permissions = permissions, SourceNames = names };
    }

    private static void CheckReportFields(ReportEditRequest request, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Enter a title.");
        }

        CheckLength("The title", request.Title, errors);
        CheckLength("The area", request.Area, errors);
        CheckLength("The confidentiality notice", request.SensitiveNotice, errors);
        CheckLength("The sort column", request.OrderBy, errors);
        if (request.PageSize is < 1 or > ReportLimits.MaxPageSize)
        {
            errors.Add($"Rows per page must be between 1 and {ReportLimits.MaxPageSize}.");
        }
    }

    private static void CheckColumns(ReportEditRequest request, ReportSourceDetail source, List<string> errors)
    {
        // A procedure SQL Server can't describe has no known columns; its columns are taken on trust.
        bool known = source.Columns.Count > 0;
        var columns = source.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        if (!request.Columns.Any(c => c.Visible && !string.IsNullOrWhiteSpace(c.SourceColumn)))
        {
            errors.Add("Show at least one column.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ReportColumnEdit column in request.Columns)
        {
            string name = column.SourceColumn.Trim();
            if (name.Length == 0)
            {
                errors.Add("Every column needs a source column.");
                continue;
            }

            if (!seen.Add(name))
            {
                errors.Add($"The column '{Echo(name)}' is listed more than once.");
            }

            if (known)
            {
                CheckSourceColumn(name, columns, errors);
            }

            CheckLength($"The heading of '{Echo(name)}'", column.Label, errors);
        }

        string? orderBy = request.OrderBy?.Trim();
        if (known && !string.IsNullOrEmpty(orderBy) && !columns.ContainsKey(orderBy))
        {
            errors.Add($"The source has no column named '{Echo(orderBy)}' to sort by.");
        }
    }

    private static void CheckSourceColumn(string name, Dictionary<string, ReportSourceColumn> columns, List<string> errors)
    {
        if (!columns.TryGetValue(name, out ReportSourceColumn? column))
        {
            errors.Add($"The source has no column named '{Echo(name)}'.");
        }
        else if (!column.IsSupported)
        {
            errors.Add($"The column '{Echo(name)}' holds {column.SqlType} values, which a report can't show.");
        }
    }

    private async Task CheckParametersAsync(ReportEditRequest request, ReportSourceDetail source, List<string> errors, CancellationToken ct)
    {
        bool isView = source.Kind == ReportSourceKind.View;
        bool knownColumns = source.Columns.Count > 0;
        var targets = new HashSet<string>(
            isView ? source.Columns.Where(c => c.IsSupported).Select(c => c.Name) : source.Parameters.Where(p => p.IsSupported).Select(p => p.Name),
            StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<ReportSourceSummary>? sources = null;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ReportParameterEdit parameter in request.Parameters)
        {
            string name = parameter.Name.Trim();
            if (name.Length == 0)
            {
                errors.Add("Every parameter needs a name.");
                continue;
            }

            string label = $"The parameter '{Echo(name)}'";
            if (!seen.Add(name))
            {
                errors.Add($"{label} is listed more than once.");
            }

            if ((!isView || knownColumns) && !targets.Contains(name))
            {
                errors.Add(isView
                    ? $"{label} doesn't match a column of the view."
                    : $"{label} doesn't match a parameter the procedure takes.");
            }

            CheckLength($"{label}'s label", parameter.Label, errors);
            CheckLength($"{label}'s default", parameter.DefaultValue, errors);
            if (parameter.Binding != ReportParameterBinding.UserInput)
            {
                continue; // Filled in from the signed-in user; type, choices and default don't apply.
            }

            if (isView)
            {
                CheckOperator(label, parameter, errors);
            }

            IReadOnlyList<ReportChoice>? choices = null;
            if (IsChoice(parameter.Type))
            {
                sources ??= await _catalog.GetSourcesAsync(ct);
                choices = CheckChoices(label, parameter, sources, errors);
            }

            CheckDefault(label, parameter, choices, errors);
        }
    }

    private static void CheckOperator(string label, ReportParameterEdit parameter, List<string> errors)
    {
        ReportFilterOperator op = parameter.Operator ?? ReportFilterOperator.Equals;
        bool fits = op switch
        {
            ReportFilterOperator.In => parameter.Type == ReportParameterType.MultiChoice,
            ReportFilterOperator.Contains => parameter.Type == ReportParameterType.Text,
            ReportFilterOperator.From or ReportFilterOperator.To => parameter.Type is ReportParameterType.Number or ReportParameterType.Date,
            _ => parameter.Type != ReportParameterType.MultiChoice,
        };
        if (!fits)
        {
            errors.Add($"{label} can't use '{op}' with a {parameter.Type} value.");
        }
    }

    /// <summary>The fixed choices, if the parameter has them; checks that it has some way to get choices.</summary>
    private static IReadOnlyList<ReportChoice>? CheckChoices(
        string label, ReportParameterEdit parameter, IReadOnlyList<ReportSourceSummary> sources, List<string> errors)
    {
        if (parameter.ChoicesList is { Count: > 0 } list)
        {
            if (list.Any(c => string.IsNullOrWhiteSpace(c.Value)))
            {
                errors.Add($"{label} has a choice without a value.");
            }

            if (list.Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() != list.Count)
            {
                errors.Add($"{label} has the same choice more than once.");
            }

            return list;
        }

        string? view = parameter.ChoicesSource?.Trim();
        if (string.IsNullOrEmpty(view))
        {
            errors.Add($"{label} needs a list of choices or a view to read them from.");
        }
        else if (!sources.Any(s => s.Kind == ReportSourceKind.View && string.Equals(s.Name, view, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"{label}'s choices view '{Echo(view)}' isn't a view in the [report] schema.");
        }

        return null;
    }

    private static void CheckDefault(string label, ReportParameterEdit parameter, IReadOnlyList<ReportChoice>? choices, List<string> errors)
    {
        string? value = parameter.DefaultValue?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        bool valid = parameter.Type switch
        {
            ReportParameterType.Number => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            ReportParameterType.Date => DateTime.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            ReportParameterType.Boolean => bool.TryParse(value, out _),
            ReportParameterType.Choice or ReportParameterType.MultiChoice =>
                choices is null || choices.Any(c => string.Equals(c.Value, value, StringComparison.OrdinalIgnoreCase)),
            _ => true,
        };
        if (!valid)
        {
            errors.Add($"{label}'s default '{Echo(value)}' isn't a valid {parameter.Type} value.");
        }
    }

    private async Task<IReadOnlyList<string>> CheckPermissionsAsync(IReadOnlyList<string> requested, List<string> errors, CancellationToken ct)
    {
        List<string> names = [.. requested.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)];
        foreach (string tooLong in names.Where(n => n.Length > PermissionLength))
        {
            errors.Add($"The permission '{Echo(tooLong)}' is longer than {PermissionLength} characters.");
        }

        IReadOnlyDictionary<string, string> found = await _permissions.FindAsync(names, ct);
        foreach (string missing in names.Where(n => !found.ContainsKey(n)))
        {
            errors.Add($"RAPS has no permission named '{Echo(missing)}'.");
        }

        return [.. names.Where(found.ContainsKey).Select(n => found[n])];
    }

    internal static bool IsChoice(ReportParameterType type)
    {
        return type is ReportParameterType.Choice or ReportParameterType.MultiChoice;
    }

    private static void CheckLength(string what, string? value, List<string> errors)
    {
        if (value is { Length: > TextLength })
        {
            errors.Add($"{what} must be {TextLength} characters or fewer.");
        }
    }

    /// <summary>Text the user sent, cut short so an error message can't echo large input.</summary>
    private static string Echo(string value)
    {
        string trimmed = value.Trim();
        return trimmed.Length <= MaxEcho ? trimmed : trimmed[..MaxEcho] + "…";
    }
}
