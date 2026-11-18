using System.Text.Json.Serialization;

namespace Viper.Areas.ReportBuilder.Models;

/// <summary>Why a report is being run: on screen, or for one of the downloads.</summary>
public enum ReportRunMode
{
    Screen,
    ExportExcel,
    ExportPdf,
}

/// <summary>What a run asks for: parameter values, and for the screen, a page, sort and search.</summary>
public sealed record ReportRunRequest
{
    /// <summary>
    /// Parameter values by parameter name, ignoring case. Every value is a list so multi-choice
    /// parameters fit; single-valued parameters use the first entry.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Parameters { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The page to show, counting from 1. Ignored for downloads and unpaginated reports.</summary>
    [JsonRequired]
    public int Page { get; init; } = 1;

    /// <summary>Rows per page; the report's page size when not given.</summary>
    public int? PerPage { get; init; }

    /// <summary>A visible, sortable column to sort by; the report's default order when not given.</summary>
    public string? SortBy { get; init; }

    [JsonRequired]
    public bool SortDescending { get; init; }

    /// <summary>Text to find in the report's visible text columns (views only).</summary>
    public string? Search { get; init; }
}

/// <summary>A column of a report's result, as the viewer and the exports show it.</summary>
public sealed record ReportResultColumn
{
    /// <summary>The source column name; also the key of the value in each row.</summary>
    public required string Name { get; init; }

    public required string Label { get; init; }
    public required ReportColumnFormat Format { get; init; }
    public required bool Sortable { get; init; }
}

/// <summary>One page (or, for downloads, all) of a report's rows.</summary>
public sealed record ReportRunResult
{
    public required IReadOnlyList<ReportResultColumn> Columns { get; init; }

    /// <summary>Each row's values by column name.</summary>
    public required IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; }

    public required int Page { get; init; }
    public required int PerPage { get; init; }

    /// <summary>Rows across all pages; null when the source can't say (a capped procedure).</summary>
    public int? TotalRows { get; init; }

    /// <summary>True when the row cap cut the result short.</summary>
    public required bool Truncated { get; init; }
}

/// <summary>How a run ended, for the controller to turn into a status code.</summary>
public enum ReportRunStatus
{
    Ok,
    NotFound,
    Forbidden,
    Invalid,

    /// <summary>The report can't run: its source or a column is gone, or the query failed.</summary>
    Unavailable,
}

/// <summary>The result of a run, or why there is none.</summary>
public sealed record ReportRunOutcome
{
    public required ReportRunStatus Status { get; init; }
    public ReportRunResult? Result { get; init; }

    /// <summary>What a download's heading needs to say about the run; set with <see cref="Result"/>.</summary>
    public ReportRunDetails? Details { get; init; }

    /// <summary>What was wrong, for <see cref="ReportRunStatus.Invalid"/> and <see cref="ReportRunStatus.Unavailable"/>.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    internal static ReportRunOutcome Fail(ReportRunStatus status, params string[] errors)
    {
        return new ReportRunOutcome { Status = status, Errors = errors };
    }
}

/// <summary>The report's own settings and the filters a run used, for a download's heading.</summary>
public sealed record ReportRunDetails
{
    public required string Title { get; init; }

    /// <summary>The report's confidentiality notice, or null when it has none.</summary>
    public string? SensitiveNotice { get; init; }

    public required bool PdfLandscape { get; init; }
    public required IReadOnlyList<ReportAppliedFilter> Filters { get; init; }
}

/// <summary>A parameter the user filled in, by its label, with the value as text.</summary>
public sealed record ReportAppliedFilter
{
    public required string Label { get; init; }
    public required string Value { get; init; }
}

/// <summary>One choice for a choice parameter.</summary>
public sealed record ReportChoice
{
    public required string Value { get; init; }
    public required string Label { get; init; }
}
