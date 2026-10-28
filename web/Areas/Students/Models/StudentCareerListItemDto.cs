namespace Viper.Areas.Students.Models;

/// <summary>
/// A list item representation of a student's completion status for each career selection field.
/// </summary>
public class StudentCareerListItemDto : StudentCareerRowDto
{
    public bool DirectionCompleted { get; set; }
    public bool PrimaryFocusCompleted { get; set; }
    public bool SecondaryFocusCompleted { get; set; }
    public bool PostGradCompleted { get; set; }
    public bool ShortTermPlansCompleted { get; set; }
    public bool LongTermPlansCompleted { get; set; }
    public string? MentorName { get; set; }
    public DateTime? LastUpdated { get; set; }
}
