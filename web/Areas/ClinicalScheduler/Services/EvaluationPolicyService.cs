namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for determining evaluation requirements for clinical rotations.
    /// </summary>
    public class EvaluationPolicyService : IEvaluationPolicyService
    {
        public EvaluationPolicyService()
        {
        }

        /// <summary>
        /// Determines if a week requires a primary evaluator based on simple business rules:
        ///
        /// 1. If RotationWeeklyPref.Closed = 1 for the rotation and week, no primary needed
        /// 2. If weekSize = 1, every week needs a primary
        /// 3. If weekSize > 1 (2, 3, 4, etc.), the last week of each block needs a primary:
        ///    - For weekSize=2: usually the second week
        ///    - For weekSize=3: usually the third week
        ///    - For weekSize=4: usually the fourth week
        ///    - For blocks with ExtendedRotation=true weeks, no evaluation needed
        ///    - Logic: For StartWeek=false, check next week:
        ///      * If next week has ExtendedRotation=true, no primary needed
        ///      * Otherwise, primary is needed
        /// </summary>
        /// <param name="weekNumber">The week number to check</param>
        /// <param name="rotationWeeks">All weeks for the rotation in the year</param>
        /// <param name="serviceWeekSize">The WeekSize from the Service table (1, 2, 3, 4, etc.)</param>
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

            // Handle null or invalid WeekSize values
            if (!serviceWeekSize.HasValue || serviceWeekSize.Value <= 0)
            {
                // NULL or 0 WeekSize indicates undefined rotation structure
                // Default to no evaluation requirement for safety
                return false;
            }

            // Handle different WeekSize values
            if (serviceWeekSize == 1)
            {
                // Single-week rotations: every week is a complete block requiring evaluation
                return true;
            }

            // Multi-week rotations (2, 3, 4, etc.): only evaluate at the end of each block
            // A week is the last week of a block when:
            // 1. It's not a StartWeek (StartWeek=false), AND
            // 2. Either the next week is a StartWeek OR there is no next week
            if (!currentWeek.StartWeek)
            {
                var nextWeek = rotationWeeks.FirstOrDefault(w => w.WeekNum == weekNumber + 1);

                // Special case: If next week is extended, this week doesn't need evaluation
                // Extended weeks are continuation weeks that don't count for evaluation
                if (nextWeek?.ExtendedRotation == true)
                {
                    return false;
                }

                // This is the last week of the block if:
                // - There is no next week (end of schedule), OR
                // - The next week starts a new block (StartWeek=true)
                bool isLastWeekOfBlock = nextWeek == null || nextWeek.StartWeek;

                return isLastWeekOfBlock;
            }

            // This is a StartWeek (first week of block) - no evaluation needed yet
            return false;
        }
    }
}
