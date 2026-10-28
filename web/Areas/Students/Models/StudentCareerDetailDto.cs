namespace Viper.Areas.Students.Models;

/// <summary>
/// Detailed career information for a student, matching the client's expected format.
/// </summary>
public class StudentCareerDetailDto
{
    public int PersonId { get; set; }
    public required string FullName { get; set; }
    public string? ClassLevel { get; set; }
    public StudentCareerInfoDto StudentInfo { get; set; } = new StudentCareerInfoDto();
    public bool CanEdit { get; set; }
    public bool CanViewStudentList { get; set; }
    public DateTime? LastUpdated { get; set; }
}
