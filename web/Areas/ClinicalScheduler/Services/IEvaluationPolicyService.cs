namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service interface for evaluation policy logic
    /// </summary>
    public interface IEvaluationPolicyService
    {
        /// <summary>
        /// Determines if a specific week number requires a primary evaluator based on rotation configuration
        /// </summary>
        /// <param name="weekNumber">Week number to check</param>
        /// <param name="rotationWeeks">Collection of rotation week info</param>
        /// <param name="serviceMinConsecutiveWeeks">Minimum number of consecutive weeks that require evaluation</param>
        /// <param name="rotationClosed">Whether the rotation is closed for this week</param>
        /// <returns>True if a primary evaluator is required for this week</returns>
        bool RequiresPrimaryEvaluator(
            int weekNumber,
            IEnumerable<IRotationWeekInfo> rotationWeeks,
            int? serviceMinConsecutiveWeeks = null,
            bool rotationClosed = false);
    }
}
