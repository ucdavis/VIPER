namespace Viper.Models.ClinicalScheduler
{
    public class ScheduleAudit
    {
        public int ScheduleAuditId { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public string Action { get; set; } = null!;
        public string? Detail { get; set; }
        public int? InstructorScheduleId { get; set; }
        public int? RotationId { get; set; }
        public int? WeekId { get; set; }
        public string? MothraId { get; set; }

        // Navigation properties (no foreign key constraints in database)
        public virtual InstructorSchedule? InstructorSchedule { get; set; }
        public virtual Rotation? Rotation { get; set; }
        public virtual Week? Week { get; set; }
        public virtual Person? Person { get; set; }
    }
}