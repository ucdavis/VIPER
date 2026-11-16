namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>
/// One run or export of a report (<c>report.report_runLog</c>): who, when, with which
/// parameters, and how many rows.
/// </summary>
public class ReportRunLog
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public ReportRunAction? Action { get; set; }
    public string? LoginId { get; set; }
    public DateTime? RanOn { get; set; }

    /// <summary>The parameter values as JSON.</summary>
    public string? Parameters { get; set; }

    public int? RowsReturned { get; set; }

    /// <summary>How long the run took, in milliseconds.</summary>
    public int? DurationMs { get; set; }
}
