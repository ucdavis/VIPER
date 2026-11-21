namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>
/// One column of a report (<c>report.report_column</c>): which source column it shows, its
/// label, order and display format.
/// </summary>
public class ReportColumn
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string? SourceColumn { get; set; }
    public string? Label { get; set; }

    /// <summary>How the values are shown on screen and in exports (the <c>datatype</c> column).</summary>
    public ReportColumnFormat? Format { get; set; }

    public bool Sortable { get; set; } = true;
    public bool Visible { get; set; } = true;
    public int DisplayOrder { get; set; }
    public ReportDefinition? Report { get; set; }
}
