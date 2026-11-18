using System.Text.Json.Serialization;

namespace Viper.Areas.ReportBuilder.Models;

/// <summary>
/// A report as the admin form edits it: the body of a create or update, and the base of what a
/// read returns. Columns and parameters are in display order.
/// </summary>
public sealed record ReportEditRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }

    /// <summary>Groups reports in the list, such as "Effort" or "Personnel".</summary>
    public string? Area { get; init; }

    /// <summary>The view or procedure in the [report] schema.</summary>
    public required string Source { get; init; }

    [JsonRequired]
    public bool Paginate { get; init; }

    /// <summary>Rows per page; the default when null.</summary>
    public int? PageSize { get; init; }

    [JsonRequired]
    public bool AllowExcel { get; init; }

    [JsonRequired]
    public bool AllowPdf { get; init; }

    [JsonRequired]
    public bool PdfLandscape { get; init; }

    public string? SensitiveNotice { get; init; }

    /// <summary>The source column the report is sorted by until the user picks another.</summary>
    public string? OrderBy { get; init; }

    [JsonRequired]
    public bool OrderByDescending { get; init; }

    /// <summary>RAPS permission names; a user needs any one. None means admins only.</summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];

    public IReadOnlyList<ReportColumnEdit> Columns { get; init; } = [];
    public IReadOnlyList<ReportParameterEdit> Parameters { get; init; } = [];

    /// <summary>
    /// The report's last-modified time as the editor loaded it; an update made from an older copy
    /// is refused. Not used when creating.
    /// </summary>
    public DateTime? LastModifiedOn { get; init; }
}

/// <summary>One column of the report, as the admin form edits it.</summary>
public sealed record ReportColumnEdit
{
    public required string SourceColumn { get; init; }

    /// <summary>The heading; the source column's name when empty.</summary>
    public string? Label { get; init; }

    [JsonRequired]
    public ReportColumnFormat Format { get; init; }

    [JsonRequired]
    public bool Sortable { get; init; }

    [JsonRequired]
    public bool Visible { get; init; }
}

/// <summary>One parameter of the report, as the admin form edits it.</summary>
public sealed record ReportParameterEdit
{
    /// <summary>The view column to filter, or the procedure parameter without the @.</summary>
    public required string Name { get; init; }

    public string? Label { get; init; }

    [JsonRequired]
    public ReportParameterType Type { get; init; }

    /// <summary>How a view column is filtered; ignored for procedures.</summary>
    public ReportFilterOperator? Operator { get; init; }

    [JsonRequired]
    public bool Required { get; init; }

    public string? DefaultValue { get; init; }

    /// <summary>A [report] view with Value and Label columns, for choice parameters.</summary>
    public string? ChoicesSource { get; init; }

    /// <summary>A fixed list of choices, used instead of <see cref="ChoicesSource"/> when given.</summary>
    public IReadOnlyList<ReportChoice>? ChoicesList { get; init; }

    [JsonRequired]
    public ReportParameterBinding Binding { get; init; }
}

/// <summary>A saved report as the admin form loads it: the editable fields and who changed it when.</summary>
public sealed record ReportAdminDefinition
{
    public required int Id { get; init; }
    public required Guid Slug { get; init; }
    public DateTime? CreatedOn { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? ModifiedOn { get; init; }
    public string? ModifiedBy { get; init; }
    public DateTime? DeletedOn { get; init; }
    public string? DeletedBy { get; init; }

    /// <summary>
    /// The editable fields, ready to send back as the update's body; its
    /// <see cref="ReportEditRequest.LastModifiedOn"/> is this version's.
    /// </summary>
    public required ReportEditRequest Report { get; init; }

    /// <summary>Permissions the report names that RAPS no longer has; nobody holds them.</summary>
    public IReadOnlyList<string> MissingPermissions { get; init; } = [];
}

/// <summary>A report in the admin list.</summary>
public sealed record ReportAdminListItem
{
    public required int Id { get; init; }
    public required Guid Slug { get; init; }
    public required string Title { get; init; }
    public string? Area { get; init; }
    public required string Source { get; init; }
    public DateTime? ModifiedOn { get; init; }
    public string? ModifiedBy { get; init; }
    public DateTime? DeletedOn { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = [];

    /// <summary>Permissions the report names that RAPS no longer has; nobody holds them.</summary>
    public IReadOnlyList<string> MissingPermissions { get; init; } = [];
}

/// <summary>One saved version of a report.</summary>
public sealed record ReportHistoryItem
{
    public required int Id { get; init; }
    public ReportHistoryAction? Action { get; init; }
    public DateTime? ChangedOn { get; init; }
    public string? ChangedBy { get; init; }
}

/// <summary>A saved version with the report as it was then.</summary>
public sealed record ReportHistoryVersion
{
    public required ReportHistoryItem Version { get; init; }

    /// <summary>The report's fields at that time; null if the snapshot can't be read.</summary>
    public ReportEditRequest? Report { get; init; }
}

/// <summary>A RAPS permission, for the admin form's permission picker.</summary>
public sealed record ReportPermissionOption
{
    public required string Permission { get; init; }
    public string? Description { get; init; }
}

/// <summary>A preview of an unsaved report: its fields and the page to show.</summary>
public sealed record ReportPreviewRequest
{
    public required ReportEditRequest Report { get; init; }
    public ReportRunRequest Run { get; init; } = new();
}

/// <summary>How an admin request ended, for the controller to turn into a status code.</summary>
public enum ReportAdminStatus
{
    Ok,
    NotFound,
    Invalid,

    /// <summary>Someone else saved the report after the editor loaded it.</summary>
    Conflict,
}

/// <summary>The result of an admin request, or why there is none.</summary>
public sealed record ReportAdminOutcome<T>
{
    public required ReportAdminStatus Status { get; init; }
    public T? Value { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>Builds <see cref="ReportAdminOutcome{T}"/> values.</summary>
public static class ReportAdminOutcome
{
    public static ReportAdminOutcome<T> Ok<T>(T value)
    {
        return new ReportAdminOutcome<T> { Status = ReportAdminStatus.Ok, Value = value };
    }

    public static ReportAdminOutcome<T> Fail<T>(ReportAdminStatus status, params string[] errors)
    {
        return new ReportAdminOutcome<T> { Status = status, Errors = errors };
    }
}
