namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>
/// One run or export of a report (<c>report.report_runLog</c>): who, when, with which
/// parameters, and how many rows; or, for a run that failed, why.
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

    /// <summary>SQL Server's error number when the database refused the run; null otherwise.</summary>
    public int? ErrorNumber { get; set; }

    /// <summary>Why the run failed; null when it succeeded.</summary>
    public string? ErrorMessage { get; set; }
}
