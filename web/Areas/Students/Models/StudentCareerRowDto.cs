namespace Viper.Areas.Students.Models;

/// <summary>
/// The student identity columns shared by every career selection roster row, the overview and
/// the report alike.
/// </summary>
public abstract class StudentCareerRowDto
{
    public int PersonId { get; set; }
    public string RowKey { get; set; } = string.Empty;
    public bool HasDetailRoute { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ClassLevel { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
