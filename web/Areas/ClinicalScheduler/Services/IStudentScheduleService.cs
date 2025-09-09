using Viper.Areas.CTS.Models;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for student schedule data retrieval
    /// </summary>
    public interface IStudentScheduleService
    {
        /// <summary>
        /// Get student schedule with filtering options
        /// </summary>
        /// <param name="classYear">Filter by class year</param>
        /// <param name="mothraId">Filter by student MothraId</param>
        /// <param name="rotationId">Filter by rotation ID</param>
        /// <param name="serviceId">Filter by service ID</param>
        /// <param name="weekId">Filter by week ID</param>
        /// <param name="startDate">Filter by start date</param>
        /// <param name="endDate">Filter by end date</param>
        /// <returns>List of student schedules matching the criteria</returns>
        Task<List<ClinicalScheduledStudent>> GetStudentScheduleAsync(
            int? classYear = null,
            string? mothraId = null,
            int? rotationId = null,
            int? serviceId = null,
            int? weekId = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
