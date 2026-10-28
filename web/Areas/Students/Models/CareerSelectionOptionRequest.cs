using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.Students.Models;

/// <summary>
/// The body for adding or renaming a career selection dropdown option.
/// </summary>
public class CareerSelectionOptionRequest
{
    // Some columns have a max length of under 200; this is handled by the service layer.
    [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter a name.")]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;
}
