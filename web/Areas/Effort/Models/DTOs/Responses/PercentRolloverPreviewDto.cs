namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Preview of percent assignment rollover during harvest.
/// </summary>
public class PercentRolloverPreviewDto
{
    public bool IsRolloverApplicable { get; set; }
    public int SourceAcademicYear { get; set; }      // e.g., 2025 (ending year)
    public int TargetAcademicYear { get; set; }      // e.g., 2026 (ending year)
    public string SourceAcademicYearDisplay { get; set; } = string.Empty;  // "2024-2025"
    public string TargetAcademicYearDisplay { get; set; } = string.Empty;  // "2025-2026"
    public DateTime OldEndDate { get; set; }          // June 30, 2025
    public DateTime NewStartDate { get; set; }        // July 1, 2025
    public DateTime NewEndDate { get; set; }          // June 30, 2026
    public List<PercentRolloverItemPreview> Assignments { get; set; } = [];

    /// <summary>
    /// Assignments that already have a matching successor in the target year (already rolled).
    /// Shown for informational purposes in the UI.
    /// </summary>
    public List<PercentRolloverItemPreview> ExistingAssignments { get; set; } = [];

    /// <summary>
    /// Assignments excluded from rollover because the person's assignment of the same type
    /// was manually edited or deleted after harvest. Shown for informational purposes in the UI.
    /// </summary>
    public List<PercentRolloverItemPreview> ExcludedByAudit { get; set; } = [];
}

/// <summary>
/// Individual percent assignment preview for rollover.
/// </summary>
public class PercentRolloverItemPreview
{
    public int SourcePercentageId { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string MothraId { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string TypeClass { get; set; } = string.Empty;  // Admin, Clinical, Other
    public double PercentageValue { get; set; }            // As decimal (e.g., 0.50)
    public string? UnitName { get; set; }
    public string? Modifier { get; set; }
    public bool Compensated { get; set; }
    public DateTime CurrentEndDate { get; set; }
    public DateTime ProposedStartDate { get; set; }
    public DateTime ProposedEndDate { get; set; }
}
