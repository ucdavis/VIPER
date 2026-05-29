using System.ComponentModel.DataAnnotations;

namespace Viper.Models.CTS;

public class Course
{
    [Key]
    public int CourseId { get; set; }
    public string Status { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string AcademicYear { get; set; } = null!;
    public string? Crn { get; set; }
    public string CourseNum { get; set; } = null!;
}
