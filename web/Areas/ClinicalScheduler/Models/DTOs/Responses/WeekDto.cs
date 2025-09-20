using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.Areas.ClinicalScheduler.Models.DTOs.Responses
{
    /// <summary>
    /// Data transfer object representing a week in the clinical scheduler
    /// </summary>
    public class WeekDto : IRotationWeekInfo
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
