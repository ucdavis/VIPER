namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Clinical Scheduler StudentSchedule entity - maps directly to Clinical Scheduler database studentSchedule table
    /// This is distinct from CTS.StudentSchedule which is used for cross-database view access
    /// </summary>
    public class StudentSchedule
    {
        public int StudentScheduleId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string? MailId { get; set; }
        public int? Pidm { get; set; }
        public int ServiceId { get; set; }
        public int RotationId { get; set; }
        public int WeekId { get; set; }
        public string? NotGraded { get; set; }
        public string? NotEnrolled { get; set; }
        public string? MakeUp { get; set; }
        public string? Incomplete { get; set; }

        // Navigation properties
        public virtual Service Service { get; set; } = null!;
        public virtual Rotation Rotation { get; set; } = null!;
        public virtual Week Week { get; set; } = null!;
    }
}