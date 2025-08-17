namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for rotation week information needed for evaluation policy decisions
    /// </summary>
    public interface IRotationWeekInfo
    {
        /// <summary>
        /// The academic week number from the database view
        /// </summary>
        int WeekNum { get; }

        /// <summary>
        /// Whether this is an extended rotation week
        /// </summary>
        bool ExtendedRotation { get; }

        /// <summary>
        /// The graduation year this week belongs to
        /// </summary>
        int GradYear { get; }
    }

    /// <summary>
    /// Utility class for evaluation-related business rules and policies
    /// Centralizes logic for determining evaluation requirements and schedules
    /// </summary>
    public static class EvaluationPolicyService
    {
        /// <summary>
        /// Determines if a week requires a primary evaluator based on business rules
        /// New rule: Last week of rotation requires primary evaluators (excluding extended rotation weeks)
        ///
        /// Examples:
        /// - Single week rotation (Week 5): Week 5 requires evaluation
        /// - Multi-week rotation (Weeks 5,6,7): Week 7 requires evaluation
        /// - Multi-week with extended (Weeks 5,6,7 extended): Week 6 requires evaluation (last non-extended)
        /// - All extended weeks: No evaluation required
        /// </summary>
        /// <param name="weekNumber">The academic week number from the database view</param>
        /// <param name="rotationWeeks">All weeks for this rotation to determine rotation boundaries</param>
        /// <returns>True if the week requires a primary evaluator</returns>
        public static bool RequiresPrimaryEvaluator(int weekNumber, IEnumerable<IRotationWeekInfo> rotationWeeks)
        {
            if (rotationWeeks == null || !rotationWeeks.Any())
                return false;

            // Convert to list and ensure we have the data we need
            var weeks = rotationWeeks.ToList();

            // Find all weeks with the given week number (could be multiple with different grad years)
            var candidateWeeks = weeks.Where(w => w.WeekNum == weekNumber).ToList();
            if (!candidateWeeks.Any())
                return false;

            // Check each candidate week to see if any require evaluation
            foreach (var currentWeek in candidateWeeks)
            {
                // If this is an extended rotation week, it never requires evaluation
                if (currentWeek.ExtendedRotation)
                    continue;

                // Filter weeks to only include those from the same graduation year as the current week
                var sameGradYearWeeks = weeks
                    .Where(w => w.GradYear == currentWeek.GradYear)
                    .ToList();

                // Get all non-extended rotation weeks ordered by week number
                var nonExtendedWeeks = sameGradYearWeeks
                    .Where(w => !w.ExtendedRotation)
                    .OrderBy(w => w.WeekNum)
                    .ToList();

                // If there are no non-extended weeks, no evaluation needed
                if (!nonExtendedWeeks.Any())
                    continue;

                // The last non-extended week requires evaluation
                var lastNonExtendedWeek = nonExtendedWeeks[nonExtendedWeeks.Count - 1];
                if (lastNonExtendedWeek.WeekNum == weekNumber)
                    return true;
            }

            return false;
        }

        // Future extension point: Additional evaluation policies can be added here
        // Examples:
        // - public static bool RequiresSecondaryEvaluator(int weekNumber)
        // - public static EvaluationSchedule GetEvaluationSchedule(int gradYear, int rotationId)
        // - public static bool IsEvaluationWeek(int weekNumber, string termCode)
    }
}
