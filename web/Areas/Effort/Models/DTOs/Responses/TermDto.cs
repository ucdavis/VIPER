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
}
