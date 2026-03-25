namespace Viper.Models.Courses;

public partial class Terminfo
{
    public string TermAcademicYear { get; set; } = null!;

    public string TermCode { get; set; } = null!;

    public string TermDesc { get; set; } = null!;

    public string TermCollCode { get; set; } = null!;

    public DateTime TermStartDate { get; set; }

    public DateTime TermEndDate { get; set; }

    public bool TermCurrentTerm { get; set; }

    public string TermTermType { get; set; } = null!;

    public string? TermAidYear { get; set; }

    public string? TermAcademicYearDescription { get; set; }

    public bool? TermCurrentTermMulti { get; set; }
}
