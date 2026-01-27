namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for averaged percentage by type for MP Vote display.
/// Aggregates percentage assignments by type for a given academic year.
/// </summary>
public class AveragePercentByTypeDto
{
    /// <summary>
    /// Class of the percent assignment type (Admin, Clinical, Other).
    /// </summary>
    public string TypeClass { get; set; } = string.Empty;

    /// <summary>
    /// Academic year in format "YYYY-YYYY" (e.g., "2024-2025").
    /// </summary>
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>
    /// Name of the percent assignment type.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the unit if applicable.
    /// </summary>
    public string? UnitName { get; set; }

    /// <summary>
    /// Optional modifier such as "Acting" or "Interim".
    /// </summary>
    public string? Modifier { get; set; }

    /// <summary>
    /// Averaged percentage value (0-100).
    /// </summary>
    public decimal AveragedPercent { get; set; }

    /// <summary>
    /// Formatted display string (e.g., "45%").
    /// </summary>
    public string AveragedPercentDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Full description combining type, unit, and modifier.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public bool Compensated { get; set; }
}
