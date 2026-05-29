using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for adding an instructor to the Effort system.
/// The instructor must exist in AAUD/users.Person.
/// </summary>
public class CreateInstructorRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PersonId must be a positive integer")]
    public required int PersonId { get; set; }

    [Required]
    public required int TermCode { get; set; }
}
