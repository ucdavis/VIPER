using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace Viper.Classes.Utilities;

/// <summary>
/// ClosedXML helpers for the structural accessibility features Excel's
/// Accessibility Checker expects: workbook core properties, structured
/// Excel Tables (so screen readers announce column headers), and image
/// alt text. Pair with <see cref="ExcelHelper.SanitizeSheetName"/> for
/// worksheet naming. There is no PDF/UA-equivalent ISO standard for
/// spreadsheets, but the Microsoft checker is the de-facto target for
/// WCAG-derived guidance.
/// </summary>
public static partial class ExcelAccessibilityHelper
{
    public const string DefaultAuthor = "UC Davis School of Veterinary Medicine";
    public const string DefaultLanguage = "en-US";

    [GeneratedRegex(@"[^A-Za-z0-9_]")]
    private static partial Regex InvalidTableNameCharsRegex();

    /// <summary>
    /// Set the workbook properties Excel reads for accessibility checks
    /// and the title bar (Title, Subject, Author).
    /// </summary>
    public static void SetCoreProperties(
        XLWorkbook workbook,
        string title,
        string? subject = null,
        string author = DefaultAuthor)
    {
        var props = workbook.Properties;
        props.Title = title;
        props.Subject = subject ?? title;
        props.Author = author;
        props.Created = DateTime.Now;
    }

    /// <summary>
    /// Promote a data range (column-header row + body rows) to a structured
    /// Excel Table. This is what makes assistive tech announce row data in
    /// terms of column-header labels — without it, a worksheet is just a
    /// bag of cells.
    /// <para>
    /// Excel requires table names that start with a letter / underscore
    /// and contain only letters, digits, and underscores. Table names must
    /// also be unique within the workbook; pass a sheet-derived suffix
    /// (e.g. the worksheet name) if calling on multiple sheets.
    /// </para>
    /// </summary>
    public static IXLTable PromoteToAccessibleTable(IXLRange range, string tableName)
    {
        var safeName = SanitizeTableName(tableName);
        var table = range.CreateTable(safeName);
        // Default table style adds banded rows and a styled header row, which
        // also satisfies the visual-distinction requirement.
        table.Theme = XLTableTheme.TableStyleLight1;
        table.ShowAutoFilter = false;
        return table;
    }

    private static string SanitizeTableName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Table1";
        }

        var cleaned = InvalidTableNameCharsRegex().Replace(raw, "_");
        if (!char.IsLetter(cleaned[0]) && cleaned[0] != '_')
        {
            cleaned = "T_" + cleaned;
        }

        // Excel caps table names at 255 characters.
        return cleaned.Length > 255 ? cleaned[..255] : cleaned;
    }
}
