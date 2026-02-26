namespace Viper.Areas.Effort.Models.Entities;

/// <summary>
/// Represents a course in the evalHarvest database.
/// Maps to dbo.eh_Courses table.
/// </summary>
public class EhCourse
{
    public int Crn { get; set; }
    public int TermCode { get; set; }
    public int FacilitatorEvalId { get; set; }
    public string SubjCode { get; set; } = string.Empty;
    public string CrseNumb { get; set; } = string.Empty;
    public int Enrollment { get; set; }
    public string HomeDept { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Sequence { get; set; }
    public int? Respondents { get; set; }
    public bool? IsAdHoc { get; set; }
    public string? CourseType { get; set; }
}
