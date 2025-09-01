namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Clinical Scheduler Rotation entity - maps directly to Clinical Scheduler database Rotation table
    /// This is distinct from CTS.Rotation which is used for cross-database view access
    /// </summary>
    public class Rotation
    {
        public int RotId { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string CourseNumber { get; set; } = string.Empty;
        public bool Assignable { get; set; }
        public bool Active { get; set; } = true;

        // Navigation properties
        public virtual Service Service { get; set; } = null!;
        public virtual ICollection<InstructorSchedule> InstructorSchedules { get; set; } = new List<InstructorSchedule>();
        public virtual ICollection<StudentSchedule> StudentSchedules { get; set; } = new List<StudentSchedule>();
    }
}
