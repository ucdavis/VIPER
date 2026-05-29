namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Response model for adding instructor operations
    /// </summary>
    public class AddInstructorResponse
    {
        public List<InstructorScheduleResponse> Schedules { get; set; } = new();
        public List<int> ScheduleIds { get; set; } = new();
        public string? WarningMessage { get; set; }
    }
}
