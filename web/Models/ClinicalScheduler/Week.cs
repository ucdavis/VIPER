namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Clinical Scheduler Week entity - maps directly to Clinical Scheduler database Week table/view
    /// This is distinct from CTS.Week which is used for cross-database view access
    /// </summary>
    public class Week
    {
        public int WeekId { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public int WeekNumber { get; set; }
        public int TermCode { get; set; }
        public bool ExtendedRotation { get; set; }
        public bool StartWeek { get; set; }

        // Navigation properties
        public virtual ICollection<WeekGradYear> WeekGradYears { get; set; } = new List<WeekGradYear>();
        public virtual ICollection<InstructorSchedule> InstructorSchedules { get; set; } = new List<InstructorSchedule>();
        public virtual ICollection<StudentSchedule> StudentSchedules { get; set; } = new List<StudentSchedule>();
    }
}