namespace Viper.Areas.ClinicalScheduler.Constants
{
    public static class ValidationMessages
    {
        public const string InstructorScheduleIdRequired = "InstructorScheduleId must be a positive integer";
        public const string RotationIdRequired = "RotationId must be a positive integer";
        public const string WeekIdRequired = "WeekId must be a positive integer";
        public const string WeekIdsRequired = "At least one week ID must be provided";
        public const string WeekIdsMustBeUnique = "Week IDs must be unique";
        public const string MothraIdRequired = "MothraID is required";
        public const string InvalidWeekIds = "All week IDs must be positive integers";
        public const string WeekIdsRequiredForConflicts = "Week IDs are required for conflict checking";
        public const string AtLeastOneValidWeekId = "At least one valid week ID is required";
    }
}
