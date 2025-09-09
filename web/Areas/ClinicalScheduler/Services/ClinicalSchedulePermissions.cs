namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Permission constants for Clinical Scheduler area
    /// </summary>
    public static class ClinicalSchedulePermissions
    {
        /// <summary>
        /// Base permission for Clinical Scheduler area access.
        /// Required for basic access to Clinical Scheduler functionality.
        /// </summary>
        public const string Base = "SVMSecure.ClnSched";

        /// <summary>
        /// Permission to manage all clinical schedules.
        /// Grants full management access including create, read, update, and delete operations.
        /// Falls below Admin in the permission hierarchy.
        /// </summary>
        public const string Manage = "SVMSecure.ClnSched.Manage";

        /// <summary>
        /// Permission to view student schedules.
        /// Allows viewing of all student clinical schedules across all services.
        /// </summary>
        public const string ViewStudents = "SVMSecure.ClnSched.ViewStdSchedules";

        /// <summary>
        /// Permission to view clinician schedules.
        /// Allows viewing of all instructor clinical schedules across all services.
        /// </summary>
        public const string ViewClinicians = "SVMSecure.ClnSched.ViewClnSchedules";

        /// <summary>
        /// Permission to view own schedule.
        /// Allows users to view their own clinical schedule entries only.
        /// </summary>
        public const string ViewOwn = "SVMSecure.ClnSched.MySchedule";

        /// <summary>
        /// Highest level permission - grants full access to all clinical schedule operations.
        /// Permission hierarchy: Admin > Manage > EditClnSchedules > Service-specific permissions.
        /// Overrides all other permission checks.
        /// </summary>
        public const string Admin = "SVMSecure.ClnSched.Admin";

        /// <summary>
        /// Grants permission to edit all clinical schedules across all services.
        /// Falls below Admin and Manage but above service-specific permissions in the hierarchy.
        /// </summary>
        public const string EditClnSchedules = "SVMSecure.ClnSched.EditClnSchedules";

        /// <summary>
        /// Allows clinicians to edit their own schedule entries only.
        /// Does not grant permission to edit other instructors' schedules.
        /// Must be combined with ownership verification for the specific schedule entry.
        /// </summary>
        public const string EditOwnSchedule = "SVMSecure.ClnSched.EditOwnSchedule";
    }
}
