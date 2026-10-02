namespace Viper.Areas.Reports.Engine;

/// <summary>
/// A labelled measure: a grand total, a group subtotal or a summary fact.
/// </summary>
internal sealed record ReportTotalRule<TRow>(string Label, ReportMeasure<TRow> Measure, string? ColumnKey);

internal sealed record ReportFlagRule<TRow>(
    Func<TRow, bool> When, ReportFlagKind Kind, ReportTone Tone, string Label, string? ColumnKey);

/// <summary>
/// A cross-tabulation. <see cref="RowKeys"/> may return several keys for one row (for example
/// every ethnicity a person selected), in which case the row counts once under each key.
/// </summary>
internal sealed record ReportPivotRule<TRow>(
    string Title,
    Func<TRow, IEnumerable<string?>> RowKeys,
    Func<TRow, string?> ColumnKey,
    ReportMeasure<TRow> Measure);

internal sealed class ReportChartRule<TRow>
{
    public ReportChartRule(string title, ReportChartKind kind, Func<TRow, string?> category, ReportMeasure<TRow> measure)
    {
        Title = title;
        Kind = kind;
        Category = category;
        Measure = measure;
    }

    public string Title { get; }

    public ReportChartKind Kind { get; }

    public Func<TRow, string?> Category { get; }

    public ReportMeasure<TRow> Measure { get; }

    public bool PerGroup { get; set; }

    public int MinimumRows { get; set; }

    public IReadOnlyList<string> CategoryOrder { get; set; } = [];

    /// <summary>
    /// Which rows the chart counts; by default all of them.
    /// </summary>
    public Func<TRow, bool> Includes { get; set; } = _ => true;
}

internal sealed class ReportGrouping<TRow>
{
    public ReportGrouping(Func<TRow, string?> key)
    {
        Key = key;
    }

    public Func<TRow, string?> Key { get; }

    public List<ReportTotalRule<TRow>> Subtotals { get; } = [];
}

/// <summary>
/// Fluent options for the grouping declared with <see cref="ReportBuilder{TRow, TParams}.GroupBy"/>.
/// </summary>
public sealed class ReportGroupBuilder<TRow>
{
    private readonly ReportGrouping<TRow> _grouping;

    internal ReportGroupBuilder(ReportGrouping<TRow> grouping)
    {
        _grouping = grouping;
    }

    /// <summary>
    /// Adds a per-group total, placed under <paramref name="columnKey"/> when one is given.
    /// </summary>
    public ReportGroupBuilder<TRow> Subtotal(string label, ReportMeasure<TRow> measure, string? columnKey = null)
    {
        _grouping.Subtotals.Add(ReportLayoutRules.CreateTotal(label, measure, columnKey));
        return this;
    }
}

/// <summary>
/// Fluent options for a chart declared with <see cref="ReportBuilder{TRow, TParams}.Chart"/>.
/// </summary>
public sealed class ReportChartBuilder<TRow>
{
    private readonly ReportChartRule<TRow> _chart;

    internal ReportChartBuilder(ReportChartRule<TRow> chart)
    {
        _chart = chart;
    }

    /// <summary>
    /// Draws the chart once per group instead of once for the whole report, skipping groups
    /// with fewer than <paramref name="minimumRows"/> rows. Requires a grouping.
    /// </summary>
    public ReportChartBuilder<TRow> PerGroup(int minimumRows = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumRows);
        _chart.PerGroup = true;
        _chart.MinimumRows = minimumRows;
        return this;
    }

    /// <summary>
    /// Lists categories in this order (for example age brackets). Categories not in the list
    /// follow alphabetically.
    /// </summary>
    public ReportChartBuilder<TRow> CategoryOrder(params string[] categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        _chart.CategoryOrder = categories;
        return this;
    }

    /// <summary>
    /// Charts only the rows that match, for example leaving out retired faculty while the table
    /// still lists them. A per-group chart's minimum counts matching rows.
    /// </summary>
    public ReportChartBuilder<TRow> Where(Func<TRow, bool> includes)
    {
        ArgumentNullException.ThrowIfNull(includes);
        _chart.Includes = includes;
        return this;
    }
}

internal static class ReportLayoutRules
{
    public static ReportTotalRule<TRow> CreateTotal<TRow>(string label, ReportMeasure<TRow> measure, string? columnKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(measure);
        return new ReportTotalRule<TRow>(label, measure, columnKey);
    }
}
