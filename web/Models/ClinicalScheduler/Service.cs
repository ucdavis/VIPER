namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Clinical Scheduler Service entity - maps directly to Clinical Scheduler database Service table
    /// This is distinct from CTS.Service which is used for cross-database view access
    /// </summary>
    public class Service
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Rotation> Rotations { get; set; } = new List<Rotation>();
        public virtual ICollection<StudentSchedule> StudentSchedules { get; set; } = new List<StudentSchedule>();
    }
}