namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Layout declarations: grouping, totals, summary facts, highlights, pivots and charts, plus the
/// measure factories they use.
/// </summary>
public sealed partial class ReportBuilder<TRow, TParams>
{
    private readonly List<ReportTotalRule<TRow>> _totals = [];
    private readonly List<ReportTotalRule<TRow>> _summaries = [];
    private readonly List<ReportFlagRule<TRow>> _flags = [];
    private readonly List<ReportPivotRule<TRow>> _pivots = [];
    private readonly List<ReportChartRule<TRow>> _charts = [];

    internal bool ShowRowNumbers { get; private set; }

    internal bool IsSensitive { get; private set; }

    internal ReportGrouping<TRow>? Grouping { get; private set; }

    internal IReadOnlyList<ReportTotalRule<TRow>> Totals => _totals;

    internal IReadOnlyList<ReportTotalRule<TRow>> Summaries => _summaries;

    internal IReadOnlyList<ReportFlagRule<TRow>> Flags => _flags;

    internal IReadOnlyList<ReportPivotRule<TRow>> Pivots => _pivots;

    internal IReadOnlyList<ReportChartRule<TRow>> Charts => _charts;

    #region Measures

    public ReportMeasure<TRow> Count()
    {
        return new ReportMeasure<TRow>(rows => rows.Count);
    }

    /// <summary>
    /// Counts distinct non-null keys, for example people when a person can have several rows.
    /// </summary>
    public ReportMeasure<TRow> CountDistinct<TKey>(Func<TRow, TKey> key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new ReportMeasure<TRow>(rows => rows.Select(key).Where(value => value is not null).Distinct().Count());
    }

    public ReportMeasure<TRow> Sum(Func<TRow, decimal?> value)
    {
        return Numeric(value, ReportMath.Sum);
    }

    public ReportMeasure<TRow> Average(Func<TRow, decimal?> value)
    {
        return Numeric(value, ReportMath.Average);
    }

    public ReportMeasure<TRow> Minimum(Func<TRow, decimal?> value)
    {
        return Numeric(value, ReportMath.Minimum);
    }

    public ReportMeasure<TRow> Maximum(Func<TRow, decimal?> value)
    {
        return Numeric(value, ReportMath.Maximum);
    }

    public ReportMeasure<TRow> StandardDeviation(Func<TRow, decimal?> value)
    {
        return Numeric(value, ReportMath.StandardDeviation);
    }

    #endregion

    #region Layout

    public void RowNumbers()
    {
        ShowRowNumbers = true;
    }

    /// <summary>
    /// Marks the report as containing confidential personnel data (salary, demographics, visa
    /// status, birth dates). Every export of it carries a confidentiality notice.
    /// </summary>
    public void Sensitive()
    {
        IsSensitive = true;
    }

    /// <summary>
    /// Splits the rows into groups by <paramref name="key"/>, ordered alphabetically. Blank keys
    /// form one group labelled "(Not specified)". A report has at most one grouping.
    /// </summary>
    public ReportGroupBuilder<TRow> GroupBy(Func<TRow, string?> key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (Grouping is not null)
        {
            throw new InvalidOperationException("A report can be grouped only once.");
        }

        Grouping = new ReportGrouping<TRow>(key);
        return new ReportGroupBuilder<TRow>(Grouping);
    }

    public void Total(string label, ReportMeasure<TRow> measure, string? columnKey = null)
    {
        _totals.Add(ReportLayoutRules.CreateTotal(label, measure, columnKey));
    }

    /// <summary>
    /// Adds a headline fact shown above the rows. Give <paramref name="columnKey"/> when the fact
    /// reveals that column's data, so it is hidden from users who cannot see the column.
    /// </summary>
    public void Summary(string label, ReportMeasure<TRow> measure, string? columnKey = null)
    {
        _summaries.Add(ReportLayoutRules.CreateTotal(label, measure, columnKey));
    }

    /// <summary>
    /// Emphasizes matching rows, or only the <paramref name="columnKey"/> cell when one is given.
    /// </summary>
    public void Highlight(Func<TRow, bool> when, ReportTone tone, string label, string? columnKey = null)
    {
        AddFlag(when, ReportFlagKind.Highlight, tone, label, columnKey);
    }

    /// <summary>
    /// Shows <paramref name="label"/> as a badge beside the <paramref name="columnKey"/> value of matching rows.
    /// </summary>
    public void Badge(Func<TRow, bool> when, ReportTone tone, string label, string columnKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnKey);
        AddFlag(when, ReportFlagKind.Badge, tone, label, columnKey);
    }

    public void Pivot(string title, Func<TRow, string?> rows, Func<TRow, string?> columns, ReportMeasure<TRow> measure)
    {
        ArgumentNullException.ThrowIfNull(rows);
        AddPivot(title, row => [rows(row)], columns, measure);
    }

    /// <summary>
    /// A pivot whose rows come from a multi-valued field. Each row is counted under every key it
    /// returns, so totals count selections rather than rows; use <see cref="CountDistinct{TKey}"/> to count rows.
    /// </summary>
    public void PivotEach(
        string title, Func<TRow, IEnumerable<string?>> rows, Func<TRow, string?> columns, ReportMeasure<TRow> measure)
    {
        ArgumentNullException.ThrowIfNull(rows);
        AddPivot(title, rows, columns, measure);
    }

    public ReportChartBuilder<TRow> Chart(
        string title, ReportChartKind kind, Func<TRow, string?> category, ReportMeasure<TRow> measure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(measure);
        var chart = new ReportChartRule<TRow>(title, kind, category, measure);
        _charts.Add(chart);
        return new ReportChartBuilder<TRow>(chart);
    }

    #endregion

    private void EnsureValidLayout(string reportKey)
    {
        HashSet<string> columnKeys = new(_columns.Select(column => column.Key), StringComparer.Ordinal);
        string? unknown = _totals
            .Concat(_summaries)
            .Concat(Grouping?.Subtotals ?? [])
            .Select(total => total.ColumnKey)
            .Concat(_flags.Select(flag => flag.ColumnKey))
            .FirstOrDefault(key => key is not null && !columnKeys.Contains(key));
        if (unknown is not null)
        {
            throw new InvalidOperationException($"Report '{reportKey}' refers to the unknown column '{unknown}'.");
        }

        if (Grouping is null && _charts.Any(chart => chart.PerGroup))
        {
            throw new InvalidOperationException($"Report '{reportKey}' has a per-group chart but no grouping.");
        }
    }

    private static ReportMeasure<TRow> Numeric(
        Func<TRow, decimal?> value, Func<IReadOnlyCollection<decimal>, decimal?> aggregate)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ReportMeasure<TRow>(rows => aggregate([.. rows.Select(value).OfType<decimal>()]));
    }

    private void AddFlag(Func<TRow, bool> when, ReportFlagKind kind, ReportTone tone, string label, string? columnKey)
    {
        ArgumentNullException.ThrowIfNull(when);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        _flags.Add(new ReportFlagRule<TRow>(when, kind, tone, label, columnKey));
    }

    private void AddPivot(
        string title, Func<TRow, IEnumerable<string?>> rows, Func<TRow, string?> columns, ReportMeasure<TRow> measure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(measure);
        _pivots.Add(new ReportPivotRule<TRow>(title, rows, columns, measure));
    }
}
