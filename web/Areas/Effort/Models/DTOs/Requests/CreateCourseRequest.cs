using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for manually creating a course in the Effort system.
/// Used for courses that are not in Banner or need custom values.
/// </summary>
public class CreateCourseRequest
{
    [Required]
    public required int TermCode { get; set; }

    [Required]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "CRN must be exactly 5 digits")]
    public string Crn { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 1)]
    public string SubjCode { get; set; } = string.Empty;

    [Required]
    [StringLength(5, MinimumLength = 1)]
    public string CrseNumb { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 1)]
    public string SeqNumb { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Enrollment { get; set; }

    [Range(0, 99.99)]
    public decimal Units { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 1)]
    public string CustDept { get; set; } = string.Empty;
}
