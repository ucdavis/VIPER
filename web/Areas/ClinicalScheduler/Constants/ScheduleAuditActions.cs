namespace Viper.Areas.ClinicalScheduler.Constants
{
    /// <summary>
    /// Constants for schedule audit actions to prevent magic strings and ensure consistency
    /// </summary>
    public static class ScheduleAuditActions
    {
        public const string InstructorAdded = "Added to rotation";
        public const string InstructorRemoved = "Removed from rotation";
        public const string PrimaryEvaluatorSet = "Made primary evaluator";
        public const string PrimaryEvaluatorUnset = "Primary evaluator flag removed";
    }

    /// <summary>
    /// Constants for schedule audit areas
    /// </summary>
    public static class ScheduleAuditAreas
    {
        public const string Students = "Students";
        public const string Clinicians = "Clinicians";
    }
}