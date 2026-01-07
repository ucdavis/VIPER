using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing unit.
/// </summary>
public class UpdateUnitRequest
{
    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
