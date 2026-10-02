using System.Globalization;
using Viper.Areas.Reports.Engine;

namespace Viper.Areas.Reports.Exporters;

/// <summary>
/// Everything an exporter needs: the result, the filters that produced it, and whether the
/// file must carry a confidentiality notice.
/// </summary>
public sealed record ReportExport(ReportResult Result, IReadOnlyList<ReportFilter> Filters, bool Confidential);

/// <summary>
/// Turns a report result into a downloadable file. Exporters are pure and stateless, so one
/// instance serves every request.
/// </summary>
public interface IReportExporter
{
    /// <summary>
    /// The format key used in export URLs, such as "csv".
    /// </summary>
    string Format { get; }

    string ContentType { get; }

    /// <summary>
    /// The file extension, including the leading dot.
    /// </summary>
    string Extension { get; }

    byte[] Export(ReportExport request);
}

/// <summary>
/// Wording shared by every export format, so CSV, Excel and PDF files read the same.
/// </summary>
public static class ReportExportText
{
    public const string ConfidentialNotice =
        "Confidential: contains personnel information. Share only with staff authorized to see it.";

    public const string GroupHeader = "Group";

    public const string NotesHeader = "Notes";

    public const string RowNumberHeader = "#";

    public const string TotalLabel = "Total";

    private static readonly CultureInfo UsEnglish = CultureInfo.GetCultureInfo("en-US");

    // Light fills that keep black text above WCAG AA contrast. Neutral highlights add no fill.
    private static readonly Dictionary<ReportTone, string> ToneFills = new()
    {
        [ReportTone.Info] = "#DEECF9",
        [ReportTone.Positive] = "#DFF6DD",
        [ReportTone.Warning] = "#FFF4CE",
        [ReportTone.Negative] = "#FDE7E9",
        [ReportTone.Muted] = "#EDEBE9",
    };

    public static string Generated(DateTimeOffset generatedAt)
    {
        return "Generated " + generatedAt.ToString("MM/dd/yyyy h:mm tt", UsEnglish);
    }

    /// <summary>
    /// "Filters: Faculty type: Senate; Start date: 07/01/2026", or null when nothing was filtered.
    /// </summary>
    public static string? Filters(IReadOnlyList<ReportFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);
        return filters.Count == 0
            ? null
            : "Filters: " + string.Join("; ", filters.Select(filter => $"{filter.Label}: {filter.Value}"));
    }

    /// <summary>
    /// A total's value as text: formatted like its column when it has one, otherwise as a plain
    /// number with up to two decimals. A missing value is blank.
    /// </summary>
    public static string FormatTotal(ReportTotal total, IReadOnlyList<ReportColumnMetadata> columns)
    {
        ArgumentNullException.ThrowIfNull(total);
        ArgumentNullException.ThrowIfNull(columns);
        if (total.Value is not decimal value)
        {
            return string.Empty;
        }

        ReportColumnMetadata? column = columns.FirstOrDefault(candidate => candidate.Key == total.ColumnKey);
        return column is not null ? ReportValueFormatter.Format(value, column) : FormatNumber(value);
    }

    /// <summary>
    /// A pivot, chart or total number without a column format: thousands separators and up to two decimals.
    /// </summary>
    public static string FormatNumber(decimal? value)
    {
        return value?.ToString("#,##0.##", UsEnglish) ?? string.Empty;
    }

    /// <summary>
    /// The background color for a highlight tone, or null for tones that add no fill.
    /// </summary>
    public static string? ToneFill(ReportTone tone)
    {
        return ToneFills.GetValueOrDefault(tone);
    }

    /// <summary>
    /// The fill for one cell of a row. Highlights apply in the order the report declared them,
    /// so the last one covering the cell wins. A highlight covers the cell when it targets the
    /// whole row, targets this column, or targets a column the export doesn't show.
    /// <paramref name="columnKey"/> is null for cells that belong to no column, such as row numbers.
    /// </summary>
    public static string? CellFill(IReadOnlyList<ReportFlag> flags, string? columnKey, IReadOnlySet<string> columnKeys)
    {
        ArgumentNullException.ThrowIfNull(flags);
        ArgumentNullException.ThrowIfNull(columnKeys);
        return flags
            .Where(flag => flag.Kind == ReportFlagKind.Highlight
                && (flag.ColumnKey is null || flag.ColumnKey == columnKey || !columnKeys.Contains(flag.ColumnKey)))
            .Select(flag => ToneFill(flag.Tone))
            .LastOrDefault(fill => fill is not null);
    }

    /// <summary>
    /// True when any row has a highlight or badge, which adds a Notes column to exports.
    /// </summary>
    public static bool HasNotes(ReportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Groups.Any(group => group.Rows.Any(row => row.Flags.Count > 0));
    }
}
