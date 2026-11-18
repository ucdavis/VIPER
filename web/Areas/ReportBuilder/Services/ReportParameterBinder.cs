using System.Globalization;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Models.Entities;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>The signed-in user's identifiers, for parameters bound to the current user.</summary>
public sealed record ReportCurrentUser
{
    public string? LoginId { get; init; }
    public string? MothraId { get; init; }
}

/// <summary>
/// A parameter value checked and converted to its type: <see cref="string"/>,
/// <see cref="decimal"/>, <see cref="DateTime"/> (a date), <see cref="bool"/>, or a list of
/// strings for a multi-choice parameter.
/// </summary>
public sealed record BoundReportParameter
{
    /// <summary>The view column it filters, or the procedure parameter it fills.</summary>
    public required string Name { get; init; }

    public required ReportParameterType Type { get; init; }
    public ReportFilterOperator? Operator { get; init; }
    public required object Value { get; init; }

    /// <summary>True for values the user supplied, which the run log records.</summary>
    public required bool FromUser { get; init; }
}

/// <summary>The parameters ready to use, or what was wrong with the request.</summary>
public sealed record ReportBindResult
{
    public required IReadOnlyList<BoundReportParameter> Parameters { get; init; }
    public required IReadOnlyList<string> Errors { get; init; }
}

/// <summary>
/// Checks a run's parameter values against the report's parameters: rejects names the report
/// doesn't have, requires the required ones, falls back to defaults, converts each value to its
/// type, keeps choice values to the allowed list, and fills current-user parameters on the
/// server so the user can't change them.
/// </summary>
public static class ReportParameterBinder
{
    private const string DateFormat = "yyyy-MM-dd";

    public static ReportBindResult Bind(
        IEnumerable<ReportParameter> definitions,
        IReadOnlyDictionary<string, IReadOnlyList<string>> input,
        ReportCurrentUser user,
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices)
    {
        var values = new Dictionary<string, IReadOnlyList<string>>(input, StringComparer.OrdinalIgnoreCase);
        var bound = new List<BoundReportParameter>();
        var errors = new List<string>();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ReportParameter definition in definitions.Where(d => !string.IsNullOrWhiteSpace(d.Name)).OrderBy(d => d.DisplayOrder))
        {
            string name = definition.Name!;
            known.Add(name);
            values.TryGetValue(name, out IReadOnlyList<string>? given);
            BoundReportParameter? parameter = definition.Binding is null or ReportParameterBinding.UserInput
                ? BindInput(definition, given, choices, errors)
                : BindCurrentUser(definition, given, user, errors);
            if (parameter is not null)
            {
                bound.Add(parameter);
            }
        }

        errors.AddRange(values.Keys.Where(key => !known.Contains(key)).Select(key => $"This report has no parameter named '{Shorten(key)}'."));
        return new ReportBindResult { Parameters = bound, Errors = errors };
    }

    private static BoundReportParameter? BindCurrentUser(
        ReportParameter definition, IReadOnlyList<string>? given, ReportCurrentUser user, List<string> errors)
    {
        if (given is not null)
        {
            errors.Add($"{LabelOf(definition)} is set automatically and can't be supplied.");
            return null;
        }

        string? value = definition.Binding == ReportParameterBinding.CurrentUserLoginId ? user.LoginId : user.MothraId;
        if (string.IsNullOrWhiteSpace(value))
        {
            // Without the identifier the source can't limit rows to this user, so refuse to run.
            errors.Add($"{LabelOf(definition)} can't be filled in for your account.");
            return null;
        }

        return new BoundReportParameter
        {
            Name = definition.Name!,
            Type = ReportParameterType.Text,
            Operator = definition.Operator,
            Value = value,
            FromUser = false,
        };
    }

    private static BoundReportParameter? BindInput(
        ReportParameter definition,
        IReadOnlyList<string>? given,
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices,
        List<string> errors)
    {
        List<string> raw = [.. (given ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())];
        if (raw.Count == 0 && !string.IsNullOrWhiteSpace(definition.DefaultValue))
        {
            raw.Add(definition.DefaultValue.Trim());
        }

        if (raw.Count == 0)
        {
            if (definition.Required == true)
            {
                errors.Add($"{LabelOf(definition)} is required.");
            }

            return null;
        }

        ReportParameterType type = definition.Type ?? ReportParameterType.Text;
        object? value = Convert(definition, type, raw, choices, errors);
        return value is null
            ? null
            : new BoundReportParameter
            {
                Name = definition.Name!,
                Type = type,
                Operator = definition.Operator,
                Value = value,
                FromUser = true,
            };
    }

    private static object? Convert(
        ReportParameter definition,
        ReportParameterType type,
        List<string> raw,
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices,
        List<string> errors)
    {
        string label = LabelOf(definition);
        string first = raw[0];
        switch (type)
        {
            case ReportParameterType.Number:
                return decimal.TryParse(first, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number)
                    ? number
                    : Error(errors, $"{label} must be a number.");
            case ReportParameterType.Date:
                return DateTime.TryParseExact(first, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
                    ? date
                    : Error(errors, $"{label} must be a date (YYYY-MM-DD).");
            case ReportParameterType.Boolean:
                return bool.TryParse(first, out bool flag) ? flag : Error(errors, $"{label} must be true or false.");
            case ReportParameterType.Choice:
                return AllowedValues(definition, [first], choices, errors) is [{ } choice] ? choice : null;
            case ReportParameterType.MultiChoice:
                return AllowedValues(definition, raw, choices, errors);
            default:
                return first.Length <= ReportLimits.MaxTextLength
                    ? first
                    : Error(errors, $"{label} must be {ReportLimits.MaxTextLength} characters or fewer.");
        }
    }

    /// <summary>The values, if every one is an allowed choice; otherwise null and an error.</summary>
    private static IReadOnlyList<string>? AllowedValues(
        ReportParameter definition,
        IReadOnlyList<string> raw,
        IReadOnlyDictionary<string, IReadOnlyList<ReportChoice>> choices,
        List<string> errors)
    {
        if (!choices.TryGetValue(definition.Name!, out IReadOnlyList<ReportChoice>? allowed) || allowed.Count == 0)
        {
            errors.Add($"{LabelOf(definition)} has no choices to pick from.");
            return null;
        }

        var allowedValues = new HashSet<string>(allowed.Select(c => c.Value), StringComparer.OrdinalIgnoreCase);
        if (raw.Any(value => !allowedValues.Contains(value)))
        {
            errors.Add($"{LabelOf(definition)} has a value that isn't one of its choices.");
            return null;
        }

        return [.. raw.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static object? Error(List<string> errors, string message)
    {
        errors.Add(message);
        return null;
    }

    private static string LabelOf(ReportParameter definition)
    {
        return string.IsNullOrWhiteSpace(definition.Label) ? definition.Name! : definition.Label;
    }

    /// <summary>A user-supplied name, cut short so an error message can't be used to echo large input.</summary>
    private static string Shorten(string value)
    {
        const int MaxEcho = 50;
        return value.Length <= MaxEcho ? value : value[..MaxEcho] + "…";
    }
}
