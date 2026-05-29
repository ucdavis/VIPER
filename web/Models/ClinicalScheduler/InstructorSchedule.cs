namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Clinical Scheduler InstructorSchedule entity - maps directly to Clinical Scheduler database InstructorSchedule table
    /// This is distinct from CTS.InstructorSchedule which is used for cross-database view access
    /// </summary>
    public class InstructorSchedule
    {
        public int InstructorScheduleId { get; set; }
        public string MothraId { get; set; } = string.Empty;
        public int RotationId { get; set; }
        public int WeekId { get; set; }
        public bool Evaluator { get; set; }
        public string? Role { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual Rotation Rotation { get; set; } = null!;
        public virtual Week Week { get; set; } = null!;
        public virtual Person? Person { get; set; }
    }
}
