namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// Response DTO for percentage create/update operations.
/// Wraps the saved percentage data along with any validation warnings.
/// </summary>
public class PercentageSaveResponse
{
    /// <summary>
    /// The created or updated percentage assignment.
    /// </summary>
    public PercentageDto Result { get; set; } = null!;

    /// <summary>
    /// Warnings from validation that did not block save (e.g., overlapping dates, total > 100%).
    /// </summary>
    public List<string> Warnings { get; set; } = [];
}
