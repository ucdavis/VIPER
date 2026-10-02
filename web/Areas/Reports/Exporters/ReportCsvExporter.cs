using System.Text;
using Viper.Areas.Reports.Engine;
using Viper.Classes.Utilities;

namespace Viper.Areas.Reports.Exporters;

/// <summary>
/// Exports a report's rows as CSV: one header row, then one line per row with a Group column
/// for grouped reports and a Notes column for highlights. Totals, summaries, pivots and charts
/// are left out so the file stays a clean table for further analysis.
/// </summary>
public sealed class ReportCsvExporter : IReportExporter
{
    private static readonly char[] QuotedCharacters = ['"', ',', '\r', '\n'];

    public string Format => "csv";

    public string ContentType => "text/csv";

    public string Extension => ".csv";

    public byte[] Export(ReportExport request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReportResult result = request.Result;
        bool grouped = result.Groups.Any(group => group.Label is not null);
        bool hasNotes = ReportExportText.HasNotes(result);

        var csv = new StringBuilder();
        List<string> header = [];
        if (grouped)
        {
            header.Add(Text(ReportExportText.GroupHeader));
        }

        header.AddRange(result.Columns.Select(column => Text(column.Label)));
        if (hasNotes)
        {
            header.Add(Text(ReportExportText.NotesHeader));
        }

        AppendLine(csv, header);

        foreach (ReportGroupResult group in result.Groups)
        {
            IEnumerable<string> groupCells = group.Label is { } label ? [Text(label)] : [];
            foreach (ReportRowResult row in group.Rows)
            {
                IEnumerable<string> cells = groupCells.Concat(result.Columns.Select(column => Cell(row, column)));
                AppendLine(csv, hasNotes ? cells.Append(Text(ReportValueFormatter.FlagText(row))) : cells);
            }
        }

        // The byte-order mark makes Excel open the file as UTF-8 instead of the system code page.
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    /// <summary>
    /// Quotes a value when it contains a comma, quote or line break (RFC 4180).
    /// </summary>
    internal static string Quote(string value)
    {
        return value.IndexOfAny(QuotedCharacters) >= 0
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;
    }

    /// <summary>
    /// Free text is sanitized against formula injection; numbers and dates the formatter
    /// produced are safe and stay numeric when a spreadsheet opens the file.
    /// </summary>
    private static string Cell(ReportRowResult row, ReportColumnMetadata column)
    {
        string formatted = ReportValueFormatter.Format(row.Values.GetValueOrDefault(column.Key), column);
        return ReportValueFormatter.IsNumeric(column.Format) || column.Format == ReportColumnFormat.Date
            ? Quote(formatted)
            : Text(formatted);
    }

    private static string Text(string value)
    {
        return Quote(ExcelHelper.SanitizeStringCell(value));
    }

    private static void AppendLine(StringBuilder csv, IEnumerable<string> cells)
    {
        csv.Append(string.Join(",", cells)).Append("\r\n");
    }
}
