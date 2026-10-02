using System.Text.Json.Serialization;

namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Semantic emphasis for a row or cell. The client maps tones to design-system colors and
/// always shows the flag's label as well, so meaning never depends on color alone.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportTone>))]
public enum ReportTone
{
    Neutral,
    Info,
    Positive,
    Warning,
    Negative,
    Muted,
}

/// <summary>
/// A highlight styles a row or cell; a badge adds a short label next to a cell's value.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReportFlagKind>))]
public enum ReportFlagKind
{
    Highlight,
    Badge,
}

[JsonConverter(typeof(JsonStringEnumConverter<ReportChartKind>))]
public enum ReportChartKind
{
    Bar,
    Line,
    Pie,
}

/// <summary>
/// The output of a report run, shared by the on-screen view and every export format.
/// </summary>
public sealed record ReportResult(
    string Key,
    string Title,
    DateTimeOffset GeneratedAt,
    int RowCount,
    bool RowNumbers,
    IReadOnlyList<ReportColumnMetadata> Columns,
    IReadOnlyList<ReportTotal> Summary,
    IReadOnlyList<ReportGroupResult> Groups,
    IReadOnlyList<ReportTotal> Totals,
    IReadOnlyList<ReportPivotResult> Pivots,
    IReadOnlyList<ReportChartResult> Charts);

/// <summary>
/// One group of rows. An ungrouped report has a single group whose <see cref="Label"/> is null.
/// </summary>
public sealed record ReportGroupResult(
    string? Label,
    IReadOnlyList<ReportRowResult> Rows,
    IReadOnlyList<ReportTotal> Subtotals,
    IReadOnlyList<ReportChartResult> Charts);

/// <summary>
/// One row. <see cref="Values"/> is keyed by column key and holds only the columns the user can see.
/// </summary>
public sealed record ReportRowResult(int Number, IReadOnlyDictionary<string, object?> Values, IReadOnlyList<ReportFlag> Flags);

public sealed record ReportFlag(ReportFlagKind Kind, ReportTone Tone, string Label, string? ColumnKey);

/// <summary>
/// A labelled aggregate. <see cref="ColumnKey"/> places a total under a column; for summary
/// facts it only ties the value to that column's visibility.
/// </summary>
public sealed record ReportTotal(string Label, string? ColumnKey, decimal? Value);

public sealed record ReportPivotResult(
    string Title,
    IReadOnlyList<string> ColumnKeys,
    IReadOnlyList<ReportPivotRow> Rows,
    IReadOnlyList<decimal?> ColumnTotals,
    decimal? GrandTotal);

public sealed record ReportPivotRow(string Key, IReadOnlyList<decimal?> Values, decimal? Total);

public sealed record ReportChartResult(string Title, ReportChartKind Kind, IReadOnlyList<ReportChartPoint> Points);

public sealed record ReportChartPoint(string Category, decimal? Value);
