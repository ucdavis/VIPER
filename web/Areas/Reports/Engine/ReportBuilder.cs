using System.Linq.Expressions;

namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Collects the parameters, validation rules and columns a report declares in
/// <see cref="ReportDefinition{TRow, TParams}.Configure"/>.
/// </summary>
public sealed partial class ReportBuilder<TRow, TParams> where TParams : class
{
    private readonly List<ReportParameter<TParams>> _parameters = [];
    private readonly List<ReportParameterRule<TParams>> _rules = [];
    private readonly List<ReportColumn<TRow>> _columns = [];

    internal IReadOnlyList<ReportParameter<TParams>> Parameters => _parameters;

    internal IReadOnlyList<ReportParameterRule<TParams>> Rules => _rules;

    internal IReadOnlyList<ReportColumn<TRow>> Columns => _columns;

    public ReportParameterBuilder<TParams, string?> Choice(
        Expression<Func<TParams, string?>> property, string label, IReadOnlyList<ReportOption> options)
    {
        ReportParameter<TParams> parameter = AddParameter(property, label, ReportParameterType.Choice);
        parameter.SetOptions(RequireOptions(options), value => [(string)value]);
        return new ReportParameterBuilder<TParams, string?>(parameter);
    }

    public ReportParameterBuilder<TParams, IReadOnlyList<string>?> MultiChoice(
        Expression<Func<TParams, IReadOnlyList<string>?>> property, string label, IReadOnlyList<ReportOption> options)
    {
        ReportParameter<TParams> parameter = AddParameter(property, label, ReportParameterType.MultiChoice);
        parameter.SetOptions(RequireOptions(options), value => (IEnumerable<string>)value);
        return new ReportParameterBuilder<TParams, IReadOnlyList<string>?>(parameter);
    }

    public ReportParameterBuilder<TParams, DateOnly?> Date(Expression<Func<TParams, DateOnly?>> property, string label)
    {
        return new ReportParameterBuilder<TParams, DateOnly?>(AddParameter(property, label, ReportParameterType.Date));
    }

    public ReportParameterBuilder<TParams, string?> Text(Expression<Func<TParams, string?>> property, string label)
    {
        return new ReportParameterBuilder<TParams, string?>(AddParameter(property, label, ReportParameterType.Text));
    }

    public ReportParameterBuilder<TParams, int?> Number(Expression<Func<TParams, int?>> property, string label)
    {
        return new ReportParameterBuilder<TParams, int?>(AddParameter(property, label, ReportParameterType.Number));
    }

    public ReportParameterBuilder<TParams, bool?> Boolean(Expression<Func<TParams, bool?>> property, string label)
    {
        return new ReportParameterBuilder<TParams, bool?>(AddParameter(property, label, ReportParameterType.Boolean));
    }

    /// <summary>
    /// Adds a rule that spans several parameters. Rules run only after every individual
    /// parameter is valid, so <paramref name="isValid"/> can rely on required values being set.
    /// </summary>
    public void Validate(Func<TParams, bool> isValid, string message, string? parameter = null)
    {
        ArgumentNullException.ThrowIfNull(isValid);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _rules.Add(new ReportParameterRule<TParams>(isValid, message, parameter));
    }

    /// <summary>
    /// Adds a column. The key defaults to the camelCase name of the selected property; a
    /// computed selector such as <c>r => r.First + " " + r.Last</c> needs an explicit key.
    /// </summary>
    public ReportColumnBuilder<TRow> Column<TValue>(
        Expression<Func<TRow, TValue>> selector, string label, string? key = null)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (key is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
        }

        string columnKey = key
            ?? ReportExpressions.GetPropertyKey(selector)
            ?? throw new ArgumentException("A computed column needs an explicit key.", nameof(key));
        Func<TRow, TValue> getValue = selector.Compile();
        var column = new ReportColumn<TRow>(columnKey, label, row => getValue(row));
        _columns.Add(column);
        return new ReportColumnBuilder<TRow>(column);
    }

    internal void EnsureValid(string reportKey)
    {
        if (_columns.Count == 0)
        {
            throw new InvalidOperationException($"Report '{reportKey}' must define at least one column.");
        }

        ThrowOnDuplicate(_columns.Select(column => column.Key), "column", reportKey);
        ThrowOnDuplicate(_parameters.Select(parameter => parameter.Name), "parameter", reportKey);
        EnsureValidLayout(reportKey);
    }

    private ReportParameter<TParams> AddParameter<TValue>(
        Expression<Func<TParams, TValue>> property, string label, ReportParameterType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        var parameter = new ReportParameter<TParams>(ReportExpressions.GetSettableProperty(property), label, type);
        _parameters.Add(parameter);
        return parameter;
    }

    private static IReadOnlyList<ReportOption> RequireOptions(IReadOnlyList<ReportOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Count > 0
            ? options
            : throw new ArgumentException("A choice parameter needs at least one option.", nameof(options));
    }

    private static void ThrowOnDuplicate(IEnumerable<string> names, string kind, string reportKey)
    {
        IGrouping<string, string>? duplicate = names
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Report '{reportKey}' declares the {kind} '{duplicate.Key}' more than once.");
        }
    }
}
