using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.Models.ClinicalScheduler
{
    /// <summary>
    /// Represents the vWeek view from the legacy database which includes proper academic year and week number calculations
    /// </summary>
    public class VWeek : IRotationWeekInfo
    {
        public int WeekId { get; set; }
        public int WeekNum { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public bool ExtendedRotation { get; set; }
        public int TermCode { get; set; }
        public bool StartWeek { get; set; }
        public bool ForcedVacation { get; set; }
        public int GradYear { get; set; }
        public int WeekGradYearId { get; set; }
    }
}
