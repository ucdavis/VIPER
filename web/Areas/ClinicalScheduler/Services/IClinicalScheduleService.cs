using Viper.Areas.CTS.Models;
using Viper.Models.CTS;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for Clinical schedule data retrieval
    /// </summary>
    public interface IClinicalScheduleService
    {
        /// <summary>
        /// Get student schedule with filtering options
        /// </summary>
        Task<List<ClinicalScheduledStudent>> GetStudentSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Get instructor schedule with filtering options
        /// </summary>
        Task<List<InstructorSchedule>> GetInstructorSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate, bool? active);
    }
}
