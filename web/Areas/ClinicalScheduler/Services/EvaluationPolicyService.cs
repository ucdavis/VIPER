namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for determining evaluation requirements for clinical rotations.
    /// </summary>
    public class EvaluationPolicyService : IEvaluationPolicyService
    {

        /// <summary>
        /// Determines if a week requires a primary evaluator based on simple business rules:
        ///
        /// 1. If RotationWeeklyPref.Closed = 1 for the rotation and week, no primary needed
        /// 2. If weekSize = 1, every week needs a primary
        /// 3. If weekSize = 2, the last week of the rotation needs a primary:
        ///    - Usually this is the second week
        ///    - For 3-week rotations where week 3 is ExtendedRotation=true, no evaluation needed
        ///    - Logic: For StartWeek=false, check next week:
        ///      * If next week has ExtendedRotation=true, no primary needed
        ///      * Otherwise, primary is needed
        /// </summary>
        /// <param name="weekNumber">The week number to check</param>
        /// <param name="rotationWeeks">All weeks for the rotation in the year</param>
        /// <param name="serviceWeekSize">The WeekSize from the Service table (1 or 2)</param>
        /// <param name="rotationClosed">Whether the rotation is closed this week (from RotationWeeklyPref)</param>
        /// <returns>True if the week requires a primary evaluator</returns>
        public bool RequiresPrimaryEvaluator(
            int weekNumber,
            IEnumerable<IRotationWeekInfo> rotationWeeks,
            int? serviceWeekSize = null,
            bool rotationClosed = false)
        {
            // Rule 1: If rotation is closed for this week, no primary needed
            // Closed rotations are temporarily suspended and don't require evaluation
            if (rotationClosed)
            {
                return false;
            }

            // Validate input data
            if (rotationWeeks == null || !rotationWeeks.Any())
            {
                return false;
            }

            // Find the current week in the rotation schedule
            var currentWeek = rotationWeeks.FirstOrDefault(w => w.WeekNum == weekNumber);
            if (currentWeek == null)
            {
                return false; // Week not found in rotation schedule
            }

            // Extended rotation weeks are continuation weeks that don't require new evaluations
            // These are typically used for 3+ week rotations where only the core weeks need evaluation
            if (currentWeek.ExtendedRotation)
            {
                return false;
            }

            // Rule 2: For single-week rotations, every non-extended week needs a primary evaluator
            // This is because each week is a complete rotation cycle requiring evaluation
            if (serviceWeekSize == 1)
            {
                return true;
            }

            // Rule 3: For two-week rotations, only the final week of each block needs evaluation
            // This implements the evaluation policy where students are assessed at rotation completion
            if (serviceWeekSize == 2)
            {
                // Check if this is the second week of a two-week block
                if (!currentWeek.StartWeek)
                {
                    // Look ahead to handle special cases (e.g., 3-week rotations with extended final week)
                    var nextWeek = rotationWeeks.FirstOrDefault(w => w.WeekNum == weekNumber + 1);

                    // Special case: If next week is extended, this week is actually the end of the evaluated portion
                    // No primary needed since the extended week doesn't count for evaluation purposes
                    if (nextWeek?.ExtendedRotation == true)
                    {
                        return false;
                    }

                    // Standard case: This is week 2 of a 2-week block - requires primary evaluator
                    return true;
                }

                // This is the first week of a two-week block
                // First weeks are for orientation/learning, not evaluation
                return false;
            }

            // For any other WeekSize configuration or missing data, err on the side of no evaluation requirement
            // This provides a safe default for edge cases or new rotation configurations
            return false;
        }
    }
}
