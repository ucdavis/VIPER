using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing percentage assignment.
/// PersonId cannot be changed after creation.
/// </summary>
public class UpdatePercentageRequest
{
    [Required]
    public required int PercentAssignTypeId { get; set; }

    public int? UnitId { get; set; }

    /// <summary>
    /// Optional modifier such as "Acting" or "Interim".
    /// </summary>
    [MaxLength(50)]
    public string? Modifier { get; set; }

    [MaxLength(100)]
    public string? Comment { get; set; }

    /// <summary>
    /// Percentage value as input (0-100). Stored in database as 0-1.
    /// </summary>
    [Required]
    [Range(0, 100)]
    public required decimal PercentageValue { get; set; }

    [Required]
    public required DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public required bool Compensated { get; set; }
}
