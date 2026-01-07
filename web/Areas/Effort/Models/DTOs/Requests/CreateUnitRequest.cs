using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Effort.Models.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new unit.
/// </summary>
public class CreateUnitRequest
{
    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;
}
