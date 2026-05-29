namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Response model for schedule conflict information
    /// </summary>
    public class ScheduleConflictResponse
    {
        public List<InstructorScheduleResponse> Conflicts { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool HasConflicts => Conflicts.Any();
    }
}
