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
        ///
        /// Evaluation logic:
        /// - If Service.WeekSize is specified: Evaluator required every N weeks (where N = WeekSize)
        ///   Example: WeekSize=1 means every week, WeekSize=2 means every 2nd week
        /// - If Service.WeekSize is null: Falls back to default logic (last non-extended week only)
        /// - Extended rotation weeks never require evaluation regardless of WeekSize
        ///
        /// Examples with WeekSize:
        /// - WeekSize=1, Weeks 5,6,7: All weeks require evaluation
        /// - WeekSize=2, Weeks 5,6,7,8: Weeks 6 and 8 require evaluation (every 2nd week)
        /// - WeekSize=null, Weeks 5,6,7: Week 7 requires evaluation (last week only)
        /// </summary>
        /// <param name="weekNumber">The academic week number from the database view</param>
        /// <param name="rotationWeeks">All weeks for this rotation to determine rotation boundaries</param>
        /// <param name="serviceWeekSize">Optional service-specific evaluation frequency in weeks</param>
        /// <returns>True if the week requires a primary evaluator</returns>
        public static bool RequiresPrimaryEvaluator(int weekNumber, IEnumerable<IRotationWeekInfo> rotationWeeks, int? serviceWeekSize = null)
        {
            if (rotationWeeks == null || !rotationWeeks.Any())
                return false;

            // Find all weeks with the given week number (could be multiple with different grad years)
            var candidateWeeks = rotationWeeks.Where(w => w.WeekNum == weekNumber).ToList();
            if (!candidateWeeks.Any())
                return false;

            // Check each candidate week to see if any require evaluation
            foreach (var currentWeek in candidateWeeks)
            {
                // If this is an extended rotation week, it never requires evaluation
                if (currentWeek.ExtendedRotation)
                    continue;

                // Filter weeks to only include those from the same graduation year as the current week
                var sameGradYearWeeks = rotationWeeks
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

                // Check if Service has WeekSize configured for interval-based evaluation
                if (serviceWeekSize.HasValue && serviceWeekSize.Value > 0)
                {
                    // BUSINESS RULE: WeekSize intervals count ONLY non-extended weeks.
                    // Extended weeks are completely skipped in the counting sequence.
                    // This means if WeekSize=2, evaluations occur at the 2nd, 4th, 6th non-extended week,
                    // regardless of any extended weeks that may fall between them chronologically.
                    // This is intentional to ensure consistent evaluation intervals based on actual
                    // rotation weeks rather than calendar weeks.

                    // Find the position of the current week in the non-extended weeks list
                    var weekIndex = nonExtendedWeeks.FindIndex(w => w.WeekNum == weekNumber);
                    if (weekIndex < 0)
                        continue; // Current week not found in non-extended weeks

                    // Position is 1-based (first week is position 1)
                    var position = weekIndex + 1;

                    // Check if this position requires evaluation based on WeekSize interval
                    // For WeekSize=1: every position (1,2,3,4...)
                    // For WeekSize=2: positions 2,4,6...
                    // For WeekSize=3: positions 3,6,9...
                    if (position % serviceWeekSize.Value == 0)
                        return true;
                }
                else
                {
                    // Default logic: Only the last non-extended week requires evaluation
                    var lastNonExtendedWeek = nonExtendedWeeks[nonExtendedWeeks.Count - 1];
                    if (lastNonExtendedWeek.WeekNum == weekNumber)
                        return true;
                }
            }

            return false;
        }
    }
}
