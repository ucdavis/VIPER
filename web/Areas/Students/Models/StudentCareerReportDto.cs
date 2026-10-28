namespace Viper.Areas.Students.Models;

/// <summary>
/// A report representation of a student's career selection, with all fields as strings for display purposes.
/// </summary>
public class StudentCareerReportDto : StudentCareerRowDto
{
    public string Direction { get; set; } = string.Empty;
    public string PrimaryFocus { get; set; } = string.Empty;
    public string SecondaryFocus { get; set; } = string.Empty;
    public string PostGrad { get; set; } = string.Empty;
    public string ShortTermPlans { get; set; } = string.Empty;
    public string LongTermPlans { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
}
