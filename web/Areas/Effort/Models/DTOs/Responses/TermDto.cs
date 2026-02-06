namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for term information in the Effort system.
/// Combines workflow status from effort.TermStatus with term metadata.
/// Status is computed from date fields to match legacy ColdFusion logic.
/// </summary>
public class TermDto
{
    /// <summary>
    /// The term code (e.g., 202410 for Fall 2024).
    /// </summary>
    public int TermCode { get; set; }

    /// <summary>
    /// Human-readable term name (e.g., "Fall 2024").
    /// </summary>
    public string TermName { get; set; } = string.Empty;

    /// <summary>
    /// Date when course data was harvested for this term.
    /// </summary>
    public DateTime? HarvestedDate { get; set; }

    /// <summary>
    /// Date when the term was opened for effort entry.
    /// </summary>
    public DateTime? OpenedDate { get; set; }

    /// <summary>
    /// Date when the term was closed.
    /// </summary>
    public DateTime? ClosedDate { get; set; }

    /// <summary>
    /// Workflow status derived from date fields (matches legacy ColdFusion logic):
    /// Closed if ClosedDate is set, Opened if OpenedDate is set,
    /// Harvested if HarvestedDate is set, otherwise Created.
    /// </summary>
    public string Status
    {
        get
        {
            if (ClosedDate.HasValue) return "Closed";
            if (OpenedDate.HasValue) return "Opened";
            if (HarvestedDate.HasValue) return "Harvested";
            return "Created";
        }
    }

    /// <summary>
    /// Whether the term is currently open for effort entry.
    /// </summary>
    public bool IsOpen => OpenedDate.HasValue && !ClosedDate.HasValue;

    /// <summary>
    /// Whether effort data can be edited (term is open).
    /// </summary>
    public bool CanEdit => IsOpen;

    // State transition properties for term management UI

    /// <summary>
    /// Whether the term can be opened (not yet opened and not closed).
    /// </summary>
    public bool CanOpen => !OpenedDate.HasValue && !ClosedDate.HasValue;

    /// <summary>
    /// Whether the term can be closed (currently open).
    /// </summary>
    public bool CanClose => IsOpen;

    /// <summary>
    /// Whether the term can be reopened (currently closed).
    /// </summary>
    public bool CanReopen => ClosedDate.HasValue;

    /// <summary>
    /// Whether the term can be reverted to unopened state (currently open).
    /// </summary>
    public bool CanUnopen => IsOpen;

    /// <summary>
    /// Whether the term can be deleted (no related persons, courses, or records).
    /// This value is populated by the service when fetching terms for management.
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Whether the term can be harvested (not yet opened and not closed).
    /// </summary>
    public bool CanHarvest => !OpenedDate.HasValue && !ClosedDate.HasValue;

    /// <summary>
    /// Whether percent assignments can be rolled over for this term (Fall terms only).
    /// This value is populated by the service when fetching terms for management.
    /// </summary>
    public bool CanRolloverPercent { get; set; }

    /// <summary>
    /// Whether clinical data can be imported for this term (semester terms with Created/Harvested/Open status).
    /// This value is populated by the service when fetching terms for management.
    /// </summary>
    public bool CanImportClinical { get; set; }
}
