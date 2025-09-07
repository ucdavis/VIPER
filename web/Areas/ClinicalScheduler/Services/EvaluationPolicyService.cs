namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for determining evaluation requirements for clinical rotations.
    /// Following KISS principles for simple, clear business logic.
    /// </summary>
    public static class EvaluationPolicyService
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
        public static bool RequiresPrimaryEvaluator(
            int weekNumber,
            IEnumerable<IRotationWeekInfo> rotationWeeks,
            int? serviceWeekSize = null,
            bool rotationClosed = false)
        {
            // Rule 1: If rotation is closed for this week, no primary needed
            if (rotationClosed)
                return false;

            if (rotationWeeks == null || !rotationWeeks.Any())
                return false;

            // Find the current week
            var currentWeek = rotationWeeks.FirstOrDefault(w => w.WeekNum == weekNumber);
            if (currentWeek == null)
                return false;

            // If this is an extended rotation week, it never needs evaluation
            if (currentWeek.ExtendedRotation)
                return false;

            // Rule 2: If weekSize = 1, every non-extended week needs a primary
            if (serviceWeekSize == 1)
                return true;

            // Rule 3: If weekSize = 2, check if this is the last week of a rotation block
            if (serviceWeekSize == 2)
            {
                // If StartWeek = false (this is not the start of a block)
                if (!currentWeek.StartWeek)
                {
                    // Check the next week
                    var nextWeek = rotationWeeks.FirstOrDefault(w => w.WeekNum == weekNumber + 1);

                    // If next week is extended, this week doesn't need primary
                    // (This handles 3-week rotations where week 3 is extended)
                    if (nextWeek?.ExtendedRotation == true)
                        return false;

                    // Otherwise, this is week 2 of a 2-week block - needs primary
                    return true;
                }

                // If StartWeek = true, this is the first week of a block
                // First weeks don't need primary evaluators
                return false;
            }

            // For any other WeekSize or missing configuration, default to no evaluation
            return false;
        }
    }
}
