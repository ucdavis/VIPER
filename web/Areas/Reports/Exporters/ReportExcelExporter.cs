using ClosedXML.Excel;
using Viper.Areas.Reports.Engine;
using Viper.Classes.Utilities;

namespace Viper.Areas.Reports.Exporters;

/// <summary>
/// Exports a report to an Excel workbook. The first sheet holds the header, summary facts,
/// each group as an accessible Excel table with its subtotals, and the grand totals. Pivots
/// get a sheet each, and chart data goes on a Charts sheet. Values stay typed (numbers, dates)
/// so the workbook can be sorted and summed.
/// </summary>
public sealed class ReportExcelExporter : IReportExporter
{
    private const string DateNumberFormat = "mm/dd/yyyy";
    private const string GeneralNumberFormat = "#,##0.##";
    private const string GroupFill = "#E8E8E8";
    private const int TitleFontSize = 14;

    public string Format => "xlsx";

    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public string Extension => ".xlsx";

    public byte[] Export(ReportExport request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReportResult result = request.Result;
        using var workbook = new XLWorkbook();
        ExcelAccessibilityHelper.SetCoreProperties(workbook, result.Title);
        var sheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var layout = new SheetLayout(result);
        IXLWorksheet sheet = workbook.Worksheets.Add(UniqueSheetName(result.Title, sheetNames));
        int row = WriteHeader(sheet, request);
        row = WriteTotals(sheet, row, result.Summary, layout);
        int tableNumber = 0;
        foreach (ReportGroupResult group in result.Groups)
        {
            row = WriteGroup(sheet, row, group, layout, ++tableNumber);
        }

        WriteTotals(sheet, row, result.Totals, layout);
        sheet.ColumnsUsed().AdjustToContents();

        foreach (ReportPivotResult pivot in result.Pivots)
        {
            WritePivot(workbook.Worksheets.Add(UniqueSheetName(pivot.Title, sheetNames)), pivot);
        }

        List<(string Title, ReportChartResult Chart)> charts =
        [
            .. result.Charts.Select(chart => (chart.Title, chart)),
            .. result.Groups.SelectMany(group => group.Charts.Select(chart => ($"{chart.Title}: {group.Label}", chart))),
        ];
        if (charts.Count > 0)
        {
            WriteCharts(workbook.Worksheets.Add(UniqueSheetName("Charts", sheetNames)), charts);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Sanitizes <paramref name="name"/> into a worksheet name that isn't already used, adding
    /// " (2)", " (3)" and so on while keeping within Excel's 31-character limit.
    /// </summary>
    internal static string UniqueSheetName(string name, HashSet<string> used)
    {
        const int maxLength = 31;
        string baseName = ExcelHelper.SanitizeSheetName(name);
        string candidate = baseName;
        int copy = 1;
        while (!used.Add(candidate))
        {
            copy++;
            string suffix = $" ({copy})";
            candidate = baseName[..Math.Min(baseName.Length, maxLength - suffix.Length)] + suffix;
        }

        return candidate;
    }

    internal static string NumberFormat(ReportColumnMetadata column)
    {
        int decimals = column.Decimals ?? 0;
        string fraction = decimals > 0 ? "." + new string('0', decimals) : string.Empty;
        return column.Format switch
        {
            ReportColumnFormat.Currency => "$#,##0" + fraction,
            ReportColumnFormat.Percent => "0" + fraction + "%",
            _ => "#,##0" + fraction,
        };
    }

    private static int WriteHeader(IXLWorksheet sheet, ReportExport request)
    {
        int row = 1;
        IXLCell title = sheet.Cell(row++, 1);
        title.Value = ExcelHelper.SanitizeStringCell(request.Result.Title);
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = TitleFontSize;
        sheet.Cell(row++, 1).Value = ReportExportText.Generated(request.Result.GeneratedAt);

        if (ReportExportText.Filters(request.Filters) is { } filters)
        {
            sheet.Cell(row++, 1).Value = ExcelHelper.SanitizeStringCell(filters);
        }

        if (request.Confidential)
        {
            IXLCell notice = sheet.Cell(row++, 1);
            notice.Value = ReportExportText.ConfidentialNotice;
            notice.Style.Font.Italic = true;
        }

        return row + 1;
    }

    private static int WriteGroup(IXLWorksheet sheet, int row, ReportGroupResult group, SheetLayout layout, int tableNumber)
    {
        if (group.Label is { } label)
        {
            IXLRange heading = sheet.Range(row, 1, row, layout.LastColumn);
            heading.FirstCell().Value = ExcelHelper.SanitizeStringCell(label);
            heading.Style.Font.Bold = true;
            heading.Style.Fill.BackgroundColor = XLColor.FromHtml(GroupFill);
            row++;
        }

        int headerRow = row;
        foreach ((string text, int column) in layout.Headers)
        {
            sheet.Cell(headerRow, column).Value = ExcelHelper.SanitizeStringCell(text);
        }

        sheet.Row(headerRow).CellsUsed().Style.Font.Bold = true;
        row++;

        foreach (ReportRowResult reportRow in group.Rows)
        {
            WriteRow(sheet, row++, reportRow, layout);
        }

        if (group.Rows.Count > 0)
        {
            ExcelAccessibilityHelper.PromoteToAccessibleTable(
                sheet.Range(headerRow, 1, row - 1, layout.LastColumn), $"ReportTable{tableNumber}");
        }

        return WriteTotals(sheet, row, group.Subtotals, layout);
    }

    private static void WriteRow(IXLWorksheet sheet, int row, ReportRowResult reportRow, SheetLayout layout)
    {
        if (layout.RowNumbers)
        {
            sheet.Cell(row, 1).Value = reportRow.Number;
        }

        foreach ((ReportColumnMetadata column, int index) in layout.Columns)
        {
            WriteValue(sheet.Cell(row, index), reportRow.Values.GetValueOrDefault(column.Key), column);
        }

        if (layout.NotesColumn is int notesColumn)
        {
            sheet.Cell(row, notesColumn).Value = ExcelHelper.SanitizeStringCell(ReportValueFormatter.FlagText(reportRow));
        }

        foreach (ReportFlag flag in reportRow.Flags.Where(flag => flag.Kind == ReportFlagKind.Highlight))
        {
            if (ReportExportText.ToneFill(flag.Tone) is not { } fill)
            {
                continue;
            }

            IXLRange target = flag.ColumnKey is { } key && layout.IndexOf(key) is int column
                ? sheet.Range(row, column, row, column)
                : sheet.Range(row, 1, row, layout.LastColumn);
            target.Style.Fill.BackgroundColor = XLColor.FromHtml(fill);
        }
    }

    private static void WriteValue(IXLCell cell, object? value, ReportColumnMetadata column)
    {
        switch (value)
        {
            case null:
                return;
            case DateOnly date:
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.NumberFormat.Format = DateNumberFormat;
                return;
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.NumberFormat.Format = DateNumberFormat;
                return;
        }

        if (ReportValueFormatter.IsNumeric(column.Format) && ReportValueFormatter.TryGetDecimal(value, out decimal number))
        {
            cell.Value = (double)number;
            cell.Style.NumberFormat.Format = NumberFormat(column);
            return;
        }

        cell.Value = ExcelHelper.SanitizeStringCell(ReportValueFormatter.Format(value, column));
    }

    /// <summary>
    /// Writes one row per total: the label in the first column and the value under the total's
    /// column, or beside the label when it has none.
    /// </summary>
    private static int WriteTotals(IXLWorksheet sheet, int row, IReadOnlyList<ReportTotal> totals, SheetLayout layout)
    {
        foreach (ReportTotal total in totals)
        {
            IXLCell label = sheet.Cell(row, 1);
            label.Value = ExcelHelper.SanitizeStringCell(total.Label);
            label.Style.Font.Bold = true;

            (int column, string format) = total.ColumnKey is { } key && layout.Find(key) is { } target
                ? (target.Index, NumberFormat(target.Column))
                : (2, GeneralNumberFormat);
            if (total.Value is decimal value)
            {
                IXLCell cell = sheet.Cell(row, column);
                cell.Value = (double)value;
                cell.Style.NumberFormat.Format = format;
                cell.Style.Font.Bold = true;
            }

            row++;
        }

        return totals.Count > 0 ? row + 1 : row;
    }

    private static void WritePivot(IXLWorksheet sheet, ReportPivotResult pivot)
    {
        IXLCell title = sheet.Cell(1, 1);
        title.Value = ExcelHelper.SanitizeStringCell(pivot.Title);
        title.Style.Font.Bold = true;

        const int headerRow = 2;
        int totalColumn = pivot.ColumnKeys.Count + 2;
        for (int index = 0; index < pivot.ColumnKeys.Count; index++)
        {
            sheet.Cell(headerRow, index + 2).Value = ExcelHelper.SanitizeStringCell(pivot.ColumnKeys[index]);
        }

        sheet.Cell(headerRow, totalColumn).Value = ReportExportText.TotalLabel;
        sheet.Row(headerRow).CellsUsed().Style.Font.Bold = true;

        int row = headerRow + 1;
        foreach (ReportPivotRow pivotRow in pivot.Rows)
        {
            sheet.Cell(row, 1).Value = ExcelHelper.SanitizeStringCell(pivotRow.Key);
            WriteNumbers(sheet, row, [.. pivotRow.Values, pivotRow.Total]);
            row++;
        }

        sheet.Cell(row, 1).Value = ReportExportText.TotalLabel;
        WriteNumbers(sheet, row, [.. pivot.ColumnTotals, pivot.GrandTotal]);
        sheet.Row(row).CellsUsed().Style.Font.Bold = true;
        sheet.ColumnsUsed().AdjustToContents();
    }

    private static void WriteCharts(IXLWorksheet sheet, List<(string Title, ReportChartResult Chart)> charts)
    {
        int row = 1;
        foreach ((string title, ReportChartResult chart) in charts)
        {
            IXLCell heading = sheet.Cell(row++, 1);
            heading.Value = ExcelHelper.SanitizeStringCell(title);
            heading.Style.Font.Bold = true;
            foreach (ReportChartPoint point in chart.Points)
            {
                sheet.Cell(row, 1).Value = ExcelHelper.SanitizeStringCell(point.Category);
                WriteNumbers(sheet, row, [point.Value]);
                row++;
            }

            row++;
        }

        sheet.ColumnsUsed().AdjustToContents();
    }

    /// <summary>
    /// Writes numbers into consecutive cells starting at column 2; nulls leave the cell blank.
    /// </summary>
    private static void WriteNumbers(IXLWorksheet sheet, int row, IReadOnlyList<decimal?> values)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (values[index] is decimal value)
            {
                IXLCell cell = sheet.Cell(row, index + 2);
                cell.Value = (double)value;
                cell.Style.NumberFormat.Format = GeneralNumberFormat;
            }
        }
    }

    /// <summary>
    /// Where each part of a row goes: an optional row-number column, the report's columns, and
    /// an optional Notes column for highlight and badge labels.
    /// </summary>
    private sealed class SheetLayout
    {
        private readonly Dictionary<string, (ReportColumnMetadata Column, int Index)> _byKey;

        public SheetLayout(ReportResult result)
        {
            RowNumbers = result.RowNumbers;
            int offset = RowNumbers ? 1 : 0;
            Columns = [.. result.Columns.Select((column, index) => (column, index + 1 + offset))];
            _byKey = Columns.ToDictionary(entry => entry.Column.Key, StringComparer.Ordinal);
            NotesColumn = ReportExportText.HasNotes(result) ? Columns.Count + 1 + offset : null;
            LastColumn = NotesColumn ?? Math.Max(Columns.Count + offset, 1);

            List<(string, int)> headers = RowNumbers ? [(ReportExportText.RowNumberHeader, 1)] : [];
            headers.AddRange(Columns.Select(entry => (entry.Column.Label, entry.Index)));
            if (NotesColumn is int notes)
            {
                headers.Add((ReportExportText.NotesHeader, notes));
            }

            Headers = headers;
        }

        public bool RowNumbers { get; }

        public List<(ReportColumnMetadata Column, int Index)> Columns { get; }

        public int? NotesColumn { get; }

        public int LastColumn { get; }

        public List<(string Text, int Column)> Headers { get; }

        public (ReportColumnMetadata Column, int Index)? Find(string key)
        {
            return _byKey.TryGetValue(key, out (ReportColumnMetadata Column, int Index) entry) ? entry : null;
        }

        public int? IndexOf(string key)
        {
            return Find(key)?.Index;
        }
    }
}
