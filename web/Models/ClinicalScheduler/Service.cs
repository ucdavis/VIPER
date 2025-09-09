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
        public string? ScheduleEditPermission { get; set; }

        /// <summary>
        /// Evaluation frequency in weeks. Determines how often a primary evaluator is required.
        /// Examples: 1 = every week, 2 = every 2 weeks, null = use default logic (last week only)
        /// </summary>
        public int? WeekSize { get; set; }

        // Navigation properties
        public virtual ICollection<Rotation> Rotations { get; set; } = new List<Rotation>();
    }
}
