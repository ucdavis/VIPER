namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for Clinical schedule permission checking
    /// </summary>
    public interface IClinicalScheduleSecurityService
    {
        /// <summary>
        /// Check access to student schedules with the given params
        /// </summary>
        bool CheckStudentScheduleParams(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Check access to instructor schedule(s) with the given params
        /// </summary>
        bool CheckInstructorScheduleParams(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate);
    }
}