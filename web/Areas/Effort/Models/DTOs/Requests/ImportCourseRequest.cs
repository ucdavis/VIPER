using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for importing a course from Banner into the Effort system.
/// </summary>
public class ImportCourseRequest
{
    [Required]
    public required int TermCode { get; set; }

    [Required]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "CRN must be exactly 5 digits")]
    public string Crn { get; set; } = string.Empty;

    /// <summary>
    /// Units for variable-unit courses. If null, uses the default (UnitLow) from Banner.
    /// For fixed-unit courses, this is ignored.
    /// </summary>
    [Range(0, 99.99)]
    public decimal? Units { get; set; }
}
