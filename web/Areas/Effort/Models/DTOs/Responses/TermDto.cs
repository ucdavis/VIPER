namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for term information in the Effort system.
/// Combines workflow status from effort.TermStatus with term metadata.
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
    /// Workflow status: Created, Harvested, Opened, or Closed.
    /// </summary>
    public string Status { get; set; } = string.Empty;

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
    /// Whether the term is currently open for effort entry.
    /// </summary>
    public bool IsOpen => Status == "Opened";

    /// <summary>
    /// Whether effort data can be edited (term is open).
    /// </summary>
    public bool CanEdit => Status == "Opened";

    // State transition properties for term management UI

    /// <summary>
    /// Whether the term can be opened (status is Created or Harvested).
    /// </summary>
    public bool CanOpen => Status is "Created" or "Harvested";

    /// <summary>
    /// Whether the term can be closed (status is Opened).
    /// </summary>
    public bool CanClose => Status == "Opened";

    /// <summary>
    /// Whether the term can be reopened (status is Closed).
    /// </summary>
    public bool CanReopen => Status == "Closed";

    /// <summary>
    /// Whether the term can be reverted to unopened state (status is Opened).
    /// </summary>
    public bool CanUnopen => Status == "Opened";

    /// <summary>
    /// Whether the term can be deleted (no related persons, courses, or records).
    /// This value is populated by the service when fetching terms for management.
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Whether the term can be harvested (status is Created or Harvested).
    /// </summary>
    public bool CanHarvest => Status is "Created" or "Harvested";
}
