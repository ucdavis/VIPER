namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Response model for instructor schedule information
    /// </summary>
    public class InstructorScheduleResponse
    {
        public int InstructorScheduleId { get; set; }
        public string MothraId { get; set; } = string.Empty;
        public int RotationId { get; set; }
        public int WeekId { get; set; }
        public bool IsPrimaryEvaluator { get; set; }
        public bool CanRemove { get; set; }
    }
}
