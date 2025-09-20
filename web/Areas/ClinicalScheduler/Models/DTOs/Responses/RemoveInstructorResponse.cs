namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Response model for removing instructor operations
    /// </summary>
    public class RemoveInstructorResponse
    {
        public bool Success { get; set; }
        public bool WasPrimaryEvaluator { get; set; }
        public string? InstructorName { get; set; }
    }
}
