using Viper.Models.CTS;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for instructor schedule data retrieval
    /// </summary>
    public interface IInstructorScheduleService
    {
        /// <summary>
        /// Get instructor schedule with filtering options
        /// </summary>
        /// <param name="classYear">Filter by class year</param>
        /// <param name="mothraId">Filter by instructor MothraId</param>
        /// <param name="rotationId">Filter by rotation ID</param>
        /// <param name="serviceId">Filter by service ID</param>
        /// <param name="weekId">Filter by week ID</param>
        /// <param name="startDate">Filter by start date</param>
        /// <param name="endDate">Filter by end date</param>
        /// <param name="active">Filter by active status</param>
        /// <returns>List of instructor schedules matching the criteria</returns>
        Task<List<InstructorSchedule>> GetInstructorScheduleAsync(
            int? classYear = null,
            string? mothraId = null,
            int? rotationId = null,
            int? serviceId = null,
            int? weekId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? active = null);
    }
}
