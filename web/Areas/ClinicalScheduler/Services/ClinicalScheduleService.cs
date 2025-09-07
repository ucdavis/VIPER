using Viper.Areas.CTS.Models;
using Viper.Models.CTS;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for clinical schedule operations - now delegates to specialized services
    /// Maintained for backward compatibility
    /// </summary>
    public class ClinicalScheduleService : IClinicalScheduleService
    {
        private readonly IStudentScheduleService _studentScheduleService;
        private readonly IInstructorScheduleService _instructorScheduleService;

        public ClinicalScheduleService(
            IStudentScheduleService studentScheduleService,
            IInstructorScheduleService instructorScheduleService)
        {
            _studentScheduleService = studentScheduleService;
            _instructorScheduleService = instructorScheduleService;
        }

        /// <summary>
        /// Delegates to StudentScheduleService for backward compatibility
        /// </summary>
        public async Task<List<ClinicalScheduledStudent>> GetStudentSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate)
        {
            return await _studentScheduleService.GetStudentScheduleAsync(classYear, mothraId, rotationId, serviceId, weekId, startDate, endDate);
        }

        /// <summary>
        /// Delegates to InstructorScheduleService for backward compatibility
        /// </summary>
        public async Task<List<InstructorSchedule>> GetInstructorSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate, bool? active)
        {
            return await _instructorScheduleService.GetInstructorScheduleAsync(classYear, mothraId, rotationId, serviceId, weekId, startDate, endDate, active);
        }

    }
}
