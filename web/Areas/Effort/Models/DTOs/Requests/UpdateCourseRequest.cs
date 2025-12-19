using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating a course in the Effort system.
/// Only enrollment, units, and custodial department can be modified.
/// </summary>
public class UpdateCourseRequest
{
    [Range(0, int.MaxValue)]
    public int Enrollment { get; set; }

    [Range(0, 99.99)]
    public decimal Units { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 1)]
    public string CustDept { get; set; } = string.Empty;
}
