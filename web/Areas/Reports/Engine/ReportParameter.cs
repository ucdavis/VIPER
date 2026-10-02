using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Viper.Areas.Reports.Engine;

/// <summary>
/// One input on a report's parameter form, bound to a property of the report's parameter type.
/// </summary>
internal sealed class ReportParameter<TParams> where TParams : class
{
    private readonly PropertyInfo _property;
    private ChoiceRule? _choices;

    public ReportParameter(PropertyInfo property, string label, ReportParameterType type)
    {
        _property = property;
        Name = ReportExpressions.ToKey(property.Name);
        Label = label;
        Type = type;
    }

    public string Name { get; }

    public string Label { get; }

    public ReportParameterType Type { get; }

    public bool Required { get; set; }

    public Func<object?>? DefaultFactory { get; set; }

    public IReadOnlyList<ReportOption> Options { get; private set; } = [];

    /// <summary>
    /// Restricts the parameter to <paramref name="options"/>. <paramref name="selectedValues"/>
    /// turns the bound value into the option values it selects, so single and multiple choice
    /// share one validation path.
    /// </summary>
    public void SetOptions(IReadOnlyList<ReportOption> options, Func<object, IEnumerable<string>> selectedValues)
    {
        Options = options;
        _choices = new ChoiceRule(
            options
                .DistinctBy(option => option.Value, StringComparer.Ordinal)
                .ToDictionary(option => option.Value, option => option.Label, StringComparer.Ordinal),
            selectedValues);
    }

    public ReportParameterMetadata ToMetadata()
    {
        return new ReportParameterMetadata(Name, Label, Type, Required, DefaultFactory?.Invoke(), Options);
    }

    public void ApplyDefault(TParams parameters)
    {
        if (DefaultFactory is not null && !HasValue(_property.GetValue(parameters)))
        {
            _property.SetValue(parameters, DefaultFactory());
        }
    }

    public ReportValidationError? Validate(TParams parameters)
    {
        object? value = _property.GetValue(parameters);
        if (!HasValue(value))
        {
            return Required ? new ReportValidationError(Name, $"{Label} is required.") : null;
        }

        if (_choices is not null && _choices.SelectedValues(value).Any(selected => !_choices.Labels.ContainsKey(selected)))
        {
            return new ReportValidationError(Name, $"{Label} contains a value that is not an allowed option.");
        }

        return null;
    }

    /// <summary>
    /// The parameter as a human-readable filter for report headers, or null when it is empty.
    /// Choice values show their option labels rather than their codes.
    /// </summary>
    public ReportFilter? Describe(TParams parameters)
    {
        object? value = _property.GetValue(parameters);
        return HasValue(value) ? new ReportFilter(Label, DescribeValue(value)) : null;
    }

    private string DescribeValue(object value)
    {
        if (_choices is not null)
        {
            return string.Join(", ", _choices.SelectedValues(value).Select(selected => _choices.Labels.GetValueOrDefault(selected, selected)));
        }

        return value switch
        {
            DateOnly date => date.ToString(ReportValueFormatter.DateFormat, CultureInfo.InvariantCulture),
            bool flag => flag ? "Yes" : "No",
            _ => string.Format(CultureInfo.InvariantCulture, "{0}", value),
        };
    }

    private static bool HasValue([NotNullWhen(true)] object? value)
    {
        return value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            IReadOnlyCollection<string> values => values.Count > 0,
            _ => true,
        };
    }

    private sealed record ChoiceRule(Dictionary<string, string> Labels, Func<object, IEnumerable<string>> SelectedValues);
}

/// <summary>
/// A validation rule that spans several parameters, such as "end date on or after start date".
/// </summary>
internal sealed record ReportParameterRule<TParams>(Func<TParams, bool> IsValid, string Message, string? Parameter);

/// <summary>
/// Fluent options for a parameter declared on a <see cref="ReportBuilder{TRow, TParams}"/>.
/// </summary>
public sealed class ReportParameterBuilder<TParams, TValue> where TParams : class
{
    private readonly ReportParameter<TParams> _parameter;

    internal ReportParameterBuilder(ReportParameter<TParams> parameter)
    {
        _parameter = parameter;
    }

    public ReportParameterBuilder<TParams, TValue> Required()
    {
        _parameter.Required = true;
        return this;
    }

    /// <summary>
    /// Supplies the value used when the caller leaves the parameter empty. The factory runs on
    /// every request, so date defaults such as "start of the current fiscal year" stay current.
    /// </summary>
    public ReportParameterBuilder<TParams, TValue> Default(Func<TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _parameter.DefaultFactory = () => factory();
        return this;
    }
}
