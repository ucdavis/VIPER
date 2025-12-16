namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for terms available to add to the Effort system.
/// Represents future terms from vwTerms not yet in effort.TermStatus.
/// </summary>
public class AvailableTermDto
{
    /// <summary>
    /// The term code (e.g., 202510 for Fall 2025).
    /// </summary>
    public int TermCode { get; set; }

    /// <summary>
    /// Human-readable term name (e.g., "Fall 2025").
    /// </summary>
    public string TermName { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the term.
    /// </summary>
    public DateTime StartDate { get; set; }
}
