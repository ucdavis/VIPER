namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// DTO for rotation information
    /// </summary>
    public class RotationDto
    {
        public int RotId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string CourseNumber { get; set; } = string.Empty;
        public int ServiceId { get; set; }
        public ServiceDto? Service { get; set; }
    }
}
