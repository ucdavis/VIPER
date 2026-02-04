namespace Viper.Models.CTS;

public class Session
{
    public int SessionId { get; set; }
    public string Status { get; set; } = null!;
    public string? Type { get; set; }
    public string? TypeDescription { get; set; }
    public string Title { get; set; } = null!;
    public string CourseTitle { get; set; } = null!;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public string AcademicYear { get; set; } = null!;
    public int TypeOrder { get; set; }
    public int PaceOrder { get; set; }
    public bool? MultiRole { get; set; }

    public virtual IEnumerable<SessionCompetency> Competencies { get; set; } = new List<SessionCompetency>();
}
