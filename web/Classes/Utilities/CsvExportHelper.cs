using System.Text;

namespace Viper.Classes.Utilities;

/// <summary>
/// Builds RFC 4180 CSV for file exports.
/// </summary>
public static class CsvExportHelper
{
    /// <summary>
    /// Writes a header row followed by one row per record, as UTF-8 with a byte order mark.
    /// The BOM is what tells Excel the file is UTF-8; without it Excel falls back to the local
    /// ANSI codepage and mangles any accented name.
    /// </summary>
    public static byte[] Build(IEnumerable<string> headers, IEnumerable<IEnumerable<string?>> rows)
    {
        var csv = new StringBuilder();
        csv.Append(Row(headers));
        foreach (var row in rows)
        {
            csv.Append(Row(row));
        }

        // GetBytes never writes the preamble itself, so the BOM is prepended here.
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. utf8.GetPreamble(), .. utf8.GetBytes(csv.ToString())];
    }

    private static string Row(IEnumerable<string?> cells) => string.Join(",", cells.Select(Cell)) + "\r\n";

    /// <summary>
    /// Quotes every cell and doubles any quote inside it. Line breaks are left alone: inside the
    /// quotes they are valid RFC 4180 and Excel reads them as a wrapped cell, which is what a
    /// multi-line career statement should look like.
    ///
    /// The value goes through <see cref="ExcelHelper.SanitizeStringCell"/> first. A CSV is opened
    /// in Excel at least as often as it is parsed, so a student-entered value starting with =, +,
    /// - or @ (even behind leading whitespace) would otherwise run as a formula (OWASP CWE-1236).
    /// Quoting the cell does not prevent this; Excel still evaluates a quoted formula.
    /// </summary>
    private static string Cell(string? value)
    {
        var sanitized = ExcelHelper.SanitizeStringCell(value);
        return $"\"{sanitized.Replace("\"", "\"\"")}\"";
    }
}
