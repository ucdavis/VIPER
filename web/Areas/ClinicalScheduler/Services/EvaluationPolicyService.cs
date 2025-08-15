namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Utility class for evaluation-related business rules and policies
    /// Centralizes logic for determining evaluation requirements and schedules
    /// </summary>
    public static class EvaluationPolicyService
    {
        /// <summary>
        /// Determines if a week requires a primary evaluator based on business rules
        /// Current rule: Even-numbered academic weeks require primary evaluators
        /// </summary>
        /// <param name="weekNumber">The academic week number from the database view</param>
        /// <returns>True if the week requires a primary evaluator</returns>
        public static bool RequiresPrimaryEvaluator(int weekNumber)
        {
            // Business rule: Even-numbered academic weeks require primary evaluators
            // This can be easily modified or made configurable in the future
            return weekNumber % 2 == 0;
        }

        // Future extension point: Additional evaluation policies can be added here
        // Examples:
        // - public static bool RequiresSecondaryEvaluator(int weekNumber)
        // - public static EvaluationSchedule GetEvaluationSchedule(int gradYear, int rotationId)
        // - public static bool IsEvaluationWeek(int weekNumber, string termCode)
    }
}
