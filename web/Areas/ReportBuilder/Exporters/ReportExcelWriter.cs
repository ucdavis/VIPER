using System.Globalization;
using ClosedXML.Excel;
using Viper.Areas.ReportBuilder.Models;
using Viper.Areas.ReportBuilder.Services;
using Viper.Classes.Utilities;

namespace Viper.Areas.ReportBuilder.Exporters;

/// <summary>
/// Writes a report to an Excel workbook: a heading (title, when it was generated, filters,
/// confidentiality notice, truncation note), then the rows as an accessible Excel table.
/// Numbers and dates stay typed so the sheet can be sorted and summed; text is guarded
/// against formula injection.
/// </summary>
public static class ReportExcelWriter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string Extension = ".xlsx";

    internal const string DateNumberFormat = "mm/dd/yyyy";
    internal const string DateTimeNumberFormat = "mm/dd/yyyy h:mm AM/PM";
    private const int TitleFontSize = 14;
    private const double MaxColumnWidth = 60;

    // Widths are measured on the heading and the first rows only; measuring 50,000 rows is slow.
    private const int WidthSampleRows = 200;

    public static byte[] Write(ReportExportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var workbook = new XLWorkbook();
        ExcelAccessibilityHelper.SetCoreProperties(workbook, document.Title);
        IXLWorksheet sheet = workbook.Worksheets.Add(ExcelHelper.SanitizeSheetName(document.Title));

        int headerRow = WriteHeading(sheet, document) + 1;
        int columnCount = document.Columns.Count;
        IReadOnlyList<string> headers = UniqueHeaders(document.Columns);
        for (int index = 0; index < columnCount; index++)
        {
            IXLCell header = sheet.Cell(headerRow, index + 1);
            header.Value = ExcelHelper.SanitizeStringCell(headers[index]);
            header.Style.Font.Bold = true;
        }

        var wholeNumbers = new bool[columnCount];
        Array.Fill(wholeNumbers, true);
        int row = headerRow;
        foreach (IReadOnlyDictionary<string, object?> values in document.Rows)
        {
            row++;
            for (int index = 0; index < columnCount; index++)
            {
                ReportResultColumn column = document.Columns[index];
                object? value = values.GetValueOrDefault(column.Name);
                wholeNumbers[index] &= WriteValue(sheet.Cell(row, index + 1), value, column.Format);
            }
        }

        if (row > headerRow)
        {
            for (int index = 0; index < columnCount; index++)
            {
                if (NumberFormat(document.Columns[index].Format, wholeNumbers[index]) is { } format)
                {
                    sheet.Range(headerRow + 1, index + 1, row, index + 1).Style.NumberFormat.Format = format;
                }
            }

            ExcelAccessibilityHelper.PromoteToAccessibleTable(sheet.Range(headerRow, 1, row, columnCount), "ReportTable");
        }

        if (columnCount > 0)
        {
            sheet.Columns(1, columnCount).AdjustToContents(headerRow, Math.Min(row, headerRow + WidthSampleRows), 0, MaxColumnWidth);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// The number format for a numeric column: whole numbers without decimals, others with two.
    /// Null for columns whose cells are formatted one by one (dates) or hold text.
    /// </summary>
    internal static string? NumberFormat(ReportColumnFormat format, bool wholeNumbers)
    {
        string fraction = wholeNumbers ? string.Empty : ".00";
        return format switch
        {
            ReportColumnFormat.Number => "#,##0" + fraction,
            ReportColumnFormat.Currency => "$#,##0.00",
            ReportColumnFormat.Percent => "0" + fraction + "%",
            _ => null,
        };
    }

    /// <summary>
    /// The column labels, with " (2)", " (3)" and so on added to repeats: an Excel table's
    /// header names must be unique.
    /// </summary>
    internal static IReadOnlyList<string> UniqueHeaders(IReadOnlyList<ReportResultColumn> columns)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var headers = new List<string>(columns.Count);
        foreach (string label in columns.Select(column => column.Label))
        {
            string candidate = label;
            int copy = 1;
            while (!used.Add(candidate))
            {
                copy++;
                candidate = string.Create(CultureInfo.InvariantCulture, $"{label} ({copy})");
            }

            headers.Add(candidate);
        }

        return headers;
    }

    private static int WriteHeading(IXLWorksheet sheet, ReportExportDocument document)
    {
        int row = 1;
        IXLCell title = sheet.Cell(row++, 1);
        title.Value = ExcelHelper.SanitizeStringCell(document.Title);
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = TitleFontSize;
        sheet.Cell(row++, 1).Value = ReportExportText.Generated(document.GeneratedAt);

        if (ReportExportText.Filters(document.Filters) is { } filters)
        {
            sheet.Cell(row++, 1).Value = ExcelHelper.SanitizeStringCell(filters);
        }

        if (document.SensitiveNotice is { } notice)
        {
            IXLCell cell = sheet.Cell(row++, 1);
            cell.Value = ExcelHelper.SanitizeStringCell(notice);
            cell.Style.Font.Italic = true;
        }

        if (document.Truncated)
        {
            sheet.Cell(row++, 1).Value = ReportExportText.Truncated();
        }

        return row;
    }

    /// <summary>
    /// Writes one value, typed where it can be. Returns false only for a number with a
    /// fraction (as the column shows it), which gives the column two decimals.
    /// </summary>
    private static bool WriteValue(IXLCell cell, object? value, ReportColumnFormat format)
    {
        if (value is null)
        {
            return true;
        }

        if (format != ReportColumnFormat.YesNo && ReportCellFormatter.TryGetDateTime(value, out DateTime dateTime))
        {
            cell.Value = dateTime;
            cell.Style.NumberFormat.Format = ReportCellFormatter.ShowsTime(format, dateTime) ? DateTimeNumberFormat : DateNumberFormat;
            return true;
        }

        if (format != ReportColumnFormat.YesNo && value is not bool && ReportCellFormatter.TryGetNumber(value, out decimal number))
        {
            cell.Value = (double)number;
            decimal shown = format == ReportColumnFormat.Percent ? number * 100 : number;
            return shown == decimal.Truncate(shown);
        }

        cell.Value = ExcelHelper.SanitizeStringCell(ReportCellFormatter.Text(value, format));
        return true;
    }
}
