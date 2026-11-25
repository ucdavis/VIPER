namespace Viper.Areas.ReportBuilder.Models;

/// <summary>A run or download that failed because of the report or its source, newest first.</summary>
public sealed record ReportFailure
{
    /// <summary>The run log entry.</summary>
    public required int Id { get; init; }

    public required int ReportId { get; init; }
    public required string ReportTitle { get; init; }

    /// <summary>When the report has since been deleted.</summary>
    public bool ReportDeleted { get; init; }

    public ReportRunAction? Action { get; init; }
    public string? LoginId { get; init; }
    public DateTime? RanOn { get; init; }

    /// <summary>The values the user chose, as JSON.</summary>
    public string? Parameters { get; init; }

    /// <summary>SQL Server's error number, when the database refused the run.</summary>
    public int? ErrorNumber { get; init; }

    public required string ErrorMessage { get; init; }
}

/// <summary>A saved report that wouldn't pass the admin form's checks today.</summary>
public sealed record ReportCheckProblem
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public string? Area { get; init; }
    public required string Source { get; init; }

    /// <summary>What the checks found; filled in by the problem service.</summary>
    public IReadOnlyList<string> Problems { get; init; } = [];
}

/// <summary>Every live report checked against the [report] catalog and RAPS.</summary>
public sealed record ReportCheckResult
{
    /// <summary>How many reports were checked.</summary>
    public required int Checked { get; init; }

    public required DateTime CheckedOn { get; init; }

    /// <summary>Only the reports with something wrong, by area and title.</summary>
    public required IReadOnlyList<ReportCheckProblem> Failing { get; init; }
}
