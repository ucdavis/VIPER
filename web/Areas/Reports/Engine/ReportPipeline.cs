namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Turns a report's rows and layout into a <see cref="ReportResult"/>. Pure: no I/O, no clock and
/// no user lookups, so every rule here is unit-testable with plain in-memory rows.
/// </summary>
internal static class ReportPipeline
{
    public const string BlankKeyLabel = "(Not specified)";

    public static ReportResult Build<TRow, TParams>(
        string key,
        string title,
        ReportBuilder<TRow, TParams> layout,
        IReadOnlyList<TRow> rows,
        ReportContext context,
        DateTimeOffset generatedAt)
        where TParams : class
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(context);

        List<ReportColumn<TRow>> columns = [.. layout.Columns.Where(column => column.IsVisible(context))];
        HashSet<string> visibleKeys = new(columns.Select(column => column.Key), StringComparer.Ordinal);
        bool IsVisible(string? columnKey) => columnKey is null || visibleKeys.Contains(columnKey);

        return new ReportResult(
            key,
            title,
            generatedAt,
            rows.Count,
            layout.ShowRowNumbers,
            [.. columns.Select(column => column.ToMetadata())],
            ComputeTotals(layout.Summaries, rows, IsVisible),
            BuildGroups(layout, rows, columns, IsVisible),
            ComputeTotals(layout.Totals, rows, IsVisible),
            [.. layout.Pivots.Select(pivot => BuildPivot(pivot, rows))],
            [.. layout.Charts.Where(chart => !chart.PerGroup).Select(chart => BuildChart(chart, rows))]);
    }

    /// <summary>
    /// Trims a grouping key and gives blank keys a visible label, so blank and whitespace-only
    /// values (common in the PPS data) land in one group.
    /// </summary>
    public static string NormalizeKey(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? BlankKeyLabel : key.Trim();
    }

    /// <summary>
    /// Orders categories: those named in <paramref name="preferred"/> first, in that order, then
    /// the rest alphabetically. Preferred categories with no data are left out.
    /// </summary>
    public static IReadOnlyList<string> OrderCategories(IEnumerable<string> categories, IReadOnlyList<string> preferred)
    {
        HashSet<string> present = new(categories, StringComparer.Ordinal);
        return [.. preferred
            .Where(present.Contains)
            .Concat(present.Except(preferred, StringComparer.Ordinal).Order(StringComparer.OrdinalIgnoreCase))];
    }

    private static List<ReportGroupResult> BuildGroups<TRow, TParams>(
        ReportBuilder<TRow, TParams> layout,
        IReadOnlyList<TRow> rows,
        List<ReportColumn<TRow>> columns,
        Func<string?, bool> isVisible)
        where TParams : class
    {
        if (layout.Grouping is not { } grouping)
        {
            return [new ReportGroupResult(null, BuildRows(layout, rows, columns, isVisible), [], [])];
        }

        return [.. rows
            .GroupBy(row => NormalizeKey(grouping.Key(row)), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildGroup(layout, grouping, group.Key, [.. group], columns, isVisible))];
    }

    private static ReportGroupResult BuildGroup<TRow, TParams>(
        ReportBuilder<TRow, TParams> layout,
        ReportGrouping<TRow> grouping,
        string label,
        List<TRow> rows,
        List<ReportColumn<TRow>> columns,
        Func<string?, bool> isVisible)
        where TParams : class
    {
        return new ReportGroupResult(
            label,
            BuildRows(layout, rows, columns, isVisible),
            ComputeTotals(grouping.Subtotals, rows, isVisible),
            [.. layout.Charts
                .Where(chart => chart.PerGroup && rows.Count(chart.Includes) >= chart.MinimumRows)
                .Select(chart => BuildChart(chart, rows))]);
    }

    private static List<ReportRowResult> BuildRows<TRow, TParams>(
        ReportBuilder<TRow, TParams> layout,
        IReadOnlyList<TRow> rows,
        List<ReportColumn<TRow>> columns,
        Func<string?, bool> isVisible)
        where TParams : class
    {
        List<ReportFlagRule<TRow>> flags = [.. layout.Flags.Where(flag => isVisible(flag.ColumnKey))];
        return [.. rows.Select((row, index) => new ReportRowResult(
            index + 1,
            columns.ToDictionary(column => column.Key, column => column.Value(row), StringComparer.Ordinal),
            [.. flags
                .Where(flag => flag.When(row))
                .Select(flag => new ReportFlag(flag.Kind, flag.Tone, flag.Label, flag.ColumnKey))]))];
    }

    private static List<ReportTotal> ComputeTotals<TRow>(
        IEnumerable<ReportTotalRule<TRow>> totals, IReadOnlyCollection<TRow> rows, Func<string?, bool> isVisible)
    {
        return [.. totals
            .Where(total => isVisible(total.ColumnKey))
            .Select(total => new ReportTotal(total.Label, total.ColumnKey, total.Measure.Compute(rows)))];
    }

    private static ReportPivotResult BuildPivot<TRow>(ReportPivotRule<TRow> pivot, IReadOnlyList<TRow> rows)
    {
        List<(string Row, string Column, TRow Item)> entries =
        [
            .. rows.SelectMany(item => pivot.RowKeys(item)
                .Select(rowKey => (NormalizeKey(rowKey), NormalizeKey(pivot.ColumnKey(item)), item))),
        ];
        IReadOnlyList<string> rowKeys = OrderCategories(entries.Select(entry => entry.Row), []);
        IReadOnlyList<string> columnKeys = OrderCategories(entries.Select(entry => entry.Column), []);
        ILookup<(string, string), TRow> byCell = entries.ToLookup(entry => (entry.Row, entry.Column), entry => entry.Item);
        ILookup<string, TRow> byRow = entries.ToLookup(entry => entry.Row, entry => entry.Item, StringComparer.Ordinal);
        ILookup<string, TRow> byColumn = entries.ToLookup(entry => entry.Column, entry => entry.Item, StringComparer.Ordinal);

        return new ReportPivotResult(
            pivot.Title,
            columnKeys,
            [.. rowKeys.Select(rowKey => new ReportPivotRow(
                rowKey,
                [.. columnKeys.Select(columnKey => pivot.Measure.Compute([.. byCell[(rowKey, columnKey)]]))],
                pivot.Measure.Compute([.. byRow[rowKey]])))],
            [.. columnKeys.Select(columnKey => pivot.Measure.Compute([.. byColumn[columnKey]]))],
            pivot.Measure.Compute([.. entries.Select(entry => entry.Item)]));
    }

    private static ReportChartResult BuildChart<TRow>(ReportChartRule<TRow> chart, IReadOnlyCollection<TRow> rows)
    {
        ILookup<string, TRow> byCategory = rows
            .Where(chart.Includes)
            .ToLookup(row => NormalizeKey(chart.Category(row)), StringComparer.Ordinal);
        IReadOnlyList<string> categories = OrderCategories(byCategory.Select(group => group.Key), chart.CategoryOrder);
        return new ReportChartResult(
            chart.Title,
            chart.Kind,
            [.. categories.Select(category => new ReportChartPoint(category, chart.Measure.Compute([.. byCategory[category]])))]);
    }
}
