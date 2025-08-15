using Viper.Models.VIPER;

namespace Viper.Models.CTS
{
    public class ScheduleAudit
    {
        public int ScheduleAuditId { get; set; }
        public int ModifiedBy { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Action { get; set; } = null!;
        public string? Detail { get; set; }
        public int? InstructorScheduleId { get; set; }
        public int? RotationId { get; set; }
        public int? WeekId { get; set; }
        public string? MothraId { get; set; }

        public virtual Person Modifier { get; set; } = null!;
        public virtual InstructorSchedule? InstructorSchedule { get; set; }
        public virtual Rotation? Rotation { get; set; }
        public virtual Week? Week { get; set; }
    }
}