namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Clinical Scheduler WeekGradYear entity - maps directly to Clinical Scheduler database weekGradYear table
    /// This is distinct from CTS.WeekGradYear which is used for cross-database view access
    /// </summary>
    public class WeekGradYear
    {
        public int WeekGradYearId { get; set; }
        public int WeekId { get; set; }
        public int GradYear { get; set; }
        public int WeekNum { get; set; }

        // Navigation properties
        public virtual Week Week { get; set; } = null!;
    }
}
