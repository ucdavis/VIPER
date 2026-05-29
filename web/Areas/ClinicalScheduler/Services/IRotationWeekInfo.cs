namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Simple interface for week information needed for evaluation decisions
    /// </summary>
    public interface IRotationWeekInfo
    {
        int WeekNum { get; }
        bool ExtendedRotation { get; }
        bool StartWeek { get; }
    }
}
