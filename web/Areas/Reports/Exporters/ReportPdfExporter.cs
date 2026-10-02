using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Viper.Areas.Reports.Engine;
using Viper.Classes.Utilities;

namespace Viper.Areas.Reports.Exporters;

/// <summary>
/// Exports a report to a landscape Letter PDF tagged for PDF/UA: a header with the title,
/// generated time, filters and confidentiality notice; summary facts; each group as a table
/// with its subtotals; grand totals; then pivots and chart data as tables. Highlights become
/// light cell fills and every flag label is listed in a Notes column.
/// </summary>
public sealed class ReportPdfExporter : IReportExporter
{
    private const float FontSize = 9f;
    private const float TitleFontSize = 13f;
    private const float FooterFontSize = 7f;
    private const float SectionSpacing = 10f;
    private const float CellPaddingVertical = 2f;
    private const float CellPaddingHorizontal = 3f;
    private const float RowNumberWidth = 28f;
    private const float NotesWidth = 2f;
    private const float PivotKeyWidth = 2f;
    private const float RuleWidth = 0.5f;
    private const string HeaderFill = "#E8E8E8";
    private const string RuleColor = "#CCCCCC";

    public string Format => "pdf";

    public string ContentType => "application/pdf";

    public string Extension => ".pdf";

    public byte[] Export(ReportExport request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReportResult result = request.Result;
        var layout = new TableLayout(result);

        return Document.Create(container => container.Page(page =>
            {
                page.Size(PageSizes.Letter.Landscape());
                page.MarginHorizontal(0.5f, Unit.Inch);
                page.MarginVertical(0.4f, Unit.Inch);
                page.DefaultTextStyle(style => style.FontSize(FontSize));
                page.Header().Element(header => ComposeHeader(header, request));
                page.Content().PaddingTop(SectionSpacing).Element(content => ComposeContent(content, result, layout));
                page.Footer().Element(footer => ComposeFooter(footer, request.Confidential));
            }))
            .WithAccessibility(result.Title)
            .GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ReportExport request)
    {
        container.Column(column =>
        {
            column.Item().SemanticHeader1().Text(request.Result.Title).SemiBold().FontSize(TitleFontSize);
            column.Item().Text(ReportExportText.Generated(request.Result.GeneratedAt));
            if (ReportExportText.Filters(request.Filters) is { } filters)
            {
                column.Item().Text(filters);
            }

            if (request.Confidential)
            {
                column.Item().Text(ReportExportText.ConfidentialNotice).Italic();
            }
        });
    }

    private static void ComposeContent(IContainer container, ReportResult result, TableLayout layout)
    {
        List<(string Title, ReportChartResult Chart)> charts =
        [
            .. result.Charts.Select(chart => (chart.Title, chart)),
            .. result.Groups.SelectMany(group => group.Charts.Select(chart => ($"{chart.Title}: {group.Label}", chart))),
        ];

        container.Column(column =>
        {
            column.Spacing(SectionSpacing);
            ComposeTotals(column, result.Summary, result.Columns);
            foreach (ReportGroupResult group in result.Groups)
            {
                column.Item().Element(item => ComposeGroup(item, group, layout, result.Columns));
            }

            ComposeTotals(column, result.Totals, result.Columns);
            foreach (ReportPivotResult pivot in result.Pivots)
            {
                column.Item().Element(item => ComposePivot(item, pivot));
            }

            foreach ((string title, ReportChartResult chart) in charts)
            {
                column.Item().Element(item => ComposeChart(item, title, chart));
            }
        });
    }

    private static void ComposeGroup(
        IContainer container, ReportGroupResult group, TableLayout layout, IReadOnlyList<ReportColumnMetadata> columns)
    {
        container.Column(column =>
        {
            if (group.Label is { } label)
            {
                column.Item().SemanticHeader2().Text(label).SemiBold();
            }

            column.Item().SemanticTable().Table(table =>
            {
                table.ColumnsDefinition(definition =>
                {
                    if (layout.RowNumbers)
                    {
                        definition.ConstantColumn(RowNumberWidth);
                    }

                    for (int index = 0; index < columns.Count; index++)
                    {
                        definition.RelativeColumn();
                    }

                    if (layout.Notes)
                    {
                        definition.RelativeColumn(NotesWidth);
                    }
                });

                table.Header(header =>
                {
                    foreach ((string text, ReportAlignment align) in layout.Headers)
                    {
                        HeaderCell(header.Cell(), text, align);
                    }
                });

                foreach (ReportRowResult row in group.Rows)
                {
                    string? rowFill = ReportExportText.CellFill(row.Flags, null, layout.ColumnKeys);
                    if (layout.RowNumbers)
                    {
                        BodyCell(table.Cell(), row.Number.ToString(CultureInfo.InvariantCulture), ReportAlignment.Right, rowFill);
                    }

                    foreach (ReportColumnMetadata reportColumn in columns)
                    {
                        BodyCell(
                            table.Cell(),
                            ReportValueFormatter.Format(row.Values.GetValueOrDefault(reportColumn.Key), reportColumn),
                            reportColumn.Align,
                            ReportExportText.CellFill(row.Flags, reportColumn.Key, layout.ColumnKeys));
                    }

                    if (layout.Notes)
                    {
                        BodyCell(table.Cell(), ReportValueFormatter.FlagText(row), ReportAlignment.Left, rowFill);
                    }
                }
            });

            ComposeTotals(column, group.Subtotals, columns);
        });
    }

    private static void ComposeTotals(
        ColumnDescriptor column, IReadOnlyList<ReportTotal> totals, IReadOnlyList<ReportColumnMetadata> columns)
    {
        foreach (ReportTotal total in totals)
        {
            column.Item().Text(text =>
            {
                text.Span(total.Label + ": ").SemiBold();
                text.Span(ReportExportText.FormatTotal(total, columns));
            });
        }
    }

    private static void ComposePivot(IContainer container, ReportPivotResult pivot)
    {
        container.Column(column =>
        {
            column.Item().SemanticHeader2().Text(pivot.Title).SemiBold();
            column.Item().SemanticTable().Table(table =>
            {
                table.ColumnsDefinition(definition =>
                {
                    definition.RelativeColumn(PivotKeyWidth);
                    for (int index = 0; index <= pivot.ColumnKeys.Count; index++)
                    {
                        definition.RelativeColumn();
                    }
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), string.Empty, ReportAlignment.Left);
                    foreach (string key in pivot.ColumnKeys)
                    {
                        HeaderCell(header.Cell(), key, ReportAlignment.Right);
                    }

                    HeaderCell(header.Cell(), ReportExportText.TotalLabel, ReportAlignment.Right);
                });

                foreach (ReportPivotRow row in pivot.Rows)
                {
                    NumberRow(table, row.Key, [.. row.Values, row.Total]);
                }

                NumberRow(table, ReportExportText.TotalLabel, [.. pivot.ColumnTotals, pivot.GrandTotal]);
            });
        });
    }

    private static void ComposeChart(IContainer container, string title, ReportChartResult chart)
    {
        container.Column(column =>
        {
            column.Item().SemanticHeader2().Text(title).SemiBold();
            column.Item().SemanticTable().Table(table =>
            {
                table.ColumnsDefinition(definition =>
                {
                    definition.RelativeColumn(PivotKeyWidth);
                    definition.RelativeColumn();
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Category", ReportAlignment.Left);
                    HeaderCell(header.Cell(), "Value", ReportAlignment.Right);
                });

                foreach (ReportChartPoint point in chart.Points)
                {
                    NumberRow(table, point.Category, [point.Value]);
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container, bool confidential)
    {
        container.Column(column =>
        {
            if (confidential)
            {
                column.Item().AlignCenter().Text(ReportExportText.ConfidentialNotice).FontSize(FooterFontSize).Italic();
            }

            // The page counter repeats on every page, so assistive technology skips it.
            column.Item().SemanticIgnore().AlignCenter().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }

    private static void NumberRow(TableDescriptor table, string label, IReadOnlyList<decimal?> values)
    {
        BodyCell(table.Cell(), label, ReportAlignment.Left, null);
        foreach (decimal? value in values)
        {
            BodyCell(table.Cell(), ReportExportText.FormatNumber(value), ReportAlignment.Right, null);
        }
    }

    private static void HeaderCell(IContainer cell, string text, ReportAlignment align)
    {
        Align(cell.Background(HeaderFill).PaddingVertical(CellPaddingVertical).PaddingHorizontal(CellPaddingHorizontal), align)
            .Text(text)
            .SemiBold();
    }

    private static void BodyCell(IContainer cell, string text, ReportAlignment align, string? fill)
    {
        IContainer styled = fill is null ? cell : cell.Background(fill);
        Align(
                styled.BorderBottom(RuleWidth).BorderColor(RuleColor)
                    .PaddingVertical(CellPaddingVertical).PaddingHorizontal(CellPaddingHorizontal),
                align)
            .Text(text);
    }

    private static IContainer Align(IContainer container, ReportAlignment align)
    {
        return align switch
        {
            ReportAlignment.Right => container.AlignRight(),
            ReportAlignment.Center => container.AlignCenter(),
            _ => container.AlignLeft(),
        };
    }

    /// <summary>
    /// Which extra columns a group table has, and the header text and alignment for each column.
    /// </summary>
    private sealed class TableLayout
    {
        public TableLayout(ReportResult result)
        {
            RowNumbers = result.RowNumbers;
            Notes = ReportExportText.HasNotes(result);
            ColumnKeys = new HashSet<string>(result.Columns.Select(column => column.Key), StringComparer.Ordinal);

            List<(string, ReportAlignment)> headers = RowNumbers ? [(ReportExportText.RowNumberHeader, ReportAlignment.Right)] : [];
            headers.AddRange(result.Columns.Select(column => (column.Label, column.Align)));
            if (Notes)
            {
                headers.Add((ReportExportText.NotesHeader, ReportAlignment.Left));
            }

            Headers = headers;
        }

        public bool RowNumbers { get; }

        public bool Notes { get; }

        public IReadOnlySet<string> ColumnKeys { get; }

        public List<(string Text, ReportAlignment Align)> Headers { get; }
    }
}
