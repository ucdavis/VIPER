namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents an effort type (e.g., LEC, LAB, CLI).
/// Maps to effort.EffortTypes table.
/// CLI is the only effort type that uses weeks instead of hours.
/// </summary>
public class EffortType
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool UsesWeeks { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether faculty/instructors can add this effort type themselves.
    /// </summary>
    public bool FacultyCanEnter { get; set; } = true;

    /// <summary>
    /// Whether this effort type is allowed on DVM courses.
    /// </summary>
    public bool AllowedOnDvm { get; set; } = true;

    /// <summary>
    /// Whether this effort type is allowed on 199/299 courses.
    /// </summary>
    public bool AllowedOn199299 { get; set; } = true;

    /// <summary>
    /// Whether this effort type is allowed on R courses.
    /// </summary>
    public bool AllowedOnRCourses { get; set; } = true;

    // Navigation properties
    public virtual ICollection<EffortRecord> Records { get; set; } = new List<EffortRecord>();
}
