namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Result of percentage assignment validation.
/// Contains errors that block save and warnings that allow save with confirmation.
/// </summary>
public class PercentageValidationResult
{
    /// <summary>
    /// True if validation passed with no blocking errors.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Errors that block save. User must fix these before saving.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Warnings that allow save with user confirmation.
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
