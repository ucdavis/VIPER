using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new term in the Effort system.
/// </summary>
public class CreateTermRequest
{
    /// <summary>
    /// The term code (e.g., 202410 for Fall 2024).
    /// Must be a valid 6-digit term code.
    /// </summary>
    [Required]
    [Range(100000, 999999, ErrorMessage = "Term code must be a 6-digit number")]
    public int TermCode { get; set; }
}
