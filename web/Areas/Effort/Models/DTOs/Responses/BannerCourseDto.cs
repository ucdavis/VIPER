namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for Banner course information retrieved from the courses database.
/// Used for course import search results.
/// </summary>
public class BannerCourseDto
{
    public string Crn { get; set; } = string.Empty;
    public string SubjCode { get; set; } = string.Empty;
    public string CrseNumb { get; set; } = string.Empty;
    public string SeqNumb { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Enrollment { get; set; }

    /// <summary>
    /// F = Fixed units, V = Variable units
    /// </summary>
    public string UnitType { get; set; } = string.Empty;

    /// <summary>
    /// Minimum units (or the fixed value for fixed-unit courses).
    /// </summary>
    public decimal UnitLow { get; set; }

    /// <summary>
    /// Maximum units (same as UnitLow for fixed-unit courses).
    /// </summary>
    public decimal UnitHigh { get; set; }

    /// <summary>
    /// Banner department code (e.g., "72030" which maps to VME).
    /// </summary>
    public string DeptCode { get; set; } = string.Empty;

    /// <summary>
    /// Combined course code (e.g., "VET 410").
    /// </summary>
    public string CourseCode => $"{SubjCode.Trim()} {CrseNumb.Trim()}";

    /// <summary>
    /// True if this is a variable-unit course.
    /// </summary>
    public bool IsVariableUnits => UnitType == "V";

    /// <summary>
    /// True if this course has already been imported into the Effort system for this term.
    /// </summary>
    public bool AlreadyImported { get; set; }

    /// <summary>
    /// List of unit values already imported for variable-unit courses.
    /// </summary>
    public List<decimal> ImportedUnitValues { get; set; } = new();
}
