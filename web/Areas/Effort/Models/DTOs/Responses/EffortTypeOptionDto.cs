namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for effort type dropdown options.
/// </summary>
public class EffortTypeOptionDto
{
    /// <summary>
    /// The effort type ID (e.g., "LEC", "LAB", "CLI").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The effort type description (e.g., "Lecture", "Laboratory", "Clinical").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this effort type uses weeks instead of hours.
    /// Note: CLI only uses weeks when termCode >= ClinicalAsWeeksStartTermCode.
    /// </summary>
    public bool UsesWeeks { get; set; }

    /// <summary>
    /// Whether this effort type is allowed on DVM courses.
    /// </summary>
    public bool AllowedOnDvm { get; set; }

    /// <summary>
    /// Whether this effort type is allowed on 199/299 courses.
    /// </summary>
    public bool AllowedOn199299 { get; set; }

    /// <summary>
    /// Whether this effort type is allowed on R courses.
    /// </summary>
    public bool AllowedOnRCourses { get; set; }
}
