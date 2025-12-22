namespace Viper.Areas.Effort.Models.DTOs.Responses;

/// <summary>
/// DTO for effort record information.
/// </summary>
public class RecordDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int PersonId { get; set; }
    public int TermCode { get; set; }
    public string SessionType { get; set; } = string.Empty;
    public int Role { get; set; }
    public string RoleDescription { get; set; } = string.Empty;
    public int? Hours { get; set; }
    public int? Weeks { get; set; }
    public string Crn { get; set; } = string.Empty;
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Effort value - either hours or weeks depending on session type.
    /// </summary>
    public int? EffortValue => Hours ?? Weeks;

    /// <summary>
    /// Label for the effort value ("hours" or "weeks").
    /// </summary>
    public string EffortLabel => Hours.HasValue ? "hours" : "weeks";
}
