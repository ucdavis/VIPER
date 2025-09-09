namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// DTO for service information
    /// </summary>
    public class ServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public int? WeekSize { get; set; }
        public string? ScheduleEditPermission { get; set; }
        public bool? UserCanEdit { get; set; }
    }
}
