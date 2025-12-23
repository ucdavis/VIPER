using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating an instructor in the Effort system.
/// </summary>
public class UpdateInstructorRequest
{
    [Required]
    [StringLength(6, MinimumLength = 2)]
    public string EffortDept { get; set; } = string.Empty;

    [StringLength(6)]
    public string EffortTitleCode { get; set; } = string.Empty;

    [StringLength(3)]
    public string? JobGroupId { get; set; }

    /// <summary>
    /// Report unit abbreviations. Stored as comma-separated string in database.
    /// </summary>
    public List<string>? ReportUnits { get; set; }

    /// <summary>
    /// Volunteer/Without Salary flag. When true, excludes instructor from M&amp;P reports.
    /// </summary>
    [Required]
    public required bool VolunteerWos { get; set; }
}
