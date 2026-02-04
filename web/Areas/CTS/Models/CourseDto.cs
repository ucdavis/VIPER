namespace Viper.Areas.CTS.Models
{
    public class CourseDto
    {
        public int CourseId { get; set; }
        public string Status { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string AcademicYear { get; set; } = null!;
        public string? Crn { get; set; }
        public string CourseNum { get; set; } = null!;

        public int? CompetencyCount { get; set; }
    }
}
