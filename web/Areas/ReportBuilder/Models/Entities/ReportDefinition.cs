namespace Viper.Areas.ReportBuilder.Models.Entities;

/// <summary>
/// One admin-defined report: which [reports] view or procedure it reads, who may view it, and
/// how it is laid out. Its <see cref="Slug"/> is the report's URL.
/// </summary>
public class ReportDefinition
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Groups reports in the catalog, such as "Effort" or "Personnel".</summary>
    public string? Area { get; set; }

    /// <summary>The view or procedure name. The schema is always [reports].</summary>
    public string SourceName { get; set; } = string.Empty;

    public ReportSourceKind SourceKind { get; set; }
    public bool Paginate { get; set; } = true;
    public int PageSize { get; set; } = 25;
    public bool AllowSearch { get; set; }
    public bool AllowExcel { get; set; }
    public bool AllowPdf { get; set; }
    public bool PdfLandscape { get; set; }
    public string? DefaultSortColumn { get; set; }
    public bool DefaultSortDescending { get; set; }

    /// <summary>Shown above the table and in exports when the data is confidential.</summary>
    public string? SensitiveNotice { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Set when soft-deleted; the report can be restored until then.</summary>
    public DateTime? DeletedOn { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>Sent back by the editor to detect a conflicting save, as CMS content does.</summary>
    public DateTime ModifiedOn { get; set; }

    public ICollection<ReportPermission> Permissions { get; set; } = [];
    public ICollection<ReportColumn> Columns { get; set; } = [];
    public ICollection<ReportParameter> Parameters { get; set; } = [];
}
