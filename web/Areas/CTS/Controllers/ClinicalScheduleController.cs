using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CTS.Models;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    /// <summary>
    /// Controller for retrieving student and instructor schedules. Note this should be moved into the clinical scheduler when that app is
    /// created, and the routes adjusted.
    /// </summary>
    [Route("/api/cts/clinicalschedule/")]
    //most people have this. each function will have more complex permission checking.
    [Permission(Allow = ClinicalSchedulePermissions.Base)]
    public class ClinicalScheduleController : ApiController
    {
        private readonly IClinicalScheduleService clinicalSchedule;
        private readonly IClinicalScheduleSecurityService clinicalScheduleSecurity;
        public ClinicalScheduleController(IClinicalScheduleService scheduleService, IClinicalScheduleSecurityService securityService)
        {
            clinicalSchedule = scheduleService;
            clinicalScheduleSecurity = securityService;
        }

        /// <summary>
        /// Get student schedule. Access should be restricted to admins, ViewStdSchedules, and students viewing their own schedule.
        /// </summary>
        /// <param name="classYear"></param>
        /// <param name="mothraId"></param>
        /// <param name="rotationId"></param>
        /// <param name="serviceId"></param>
        /// <param name="weekId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("student")]
        public async Task<ActionResult<List<ClinicalScheduledStudent>>> GetStudentSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate)
        {
            if (!clinicalScheduleSecurity.CheckStudentScheduleParams(mothraId, rotationId, serviceId, weekId, startDate, endDate))
            {
                return ForbidApi();
            }
            var schedule = await clinicalSchedule.GetStudentSchedule(classYear, mothraId, rotationId, serviceId, weekId, startDate, endDate);
            return schedule;
        }

        /// <summary>
        /// Get instructor schedule. Access should be restricted to admins, ViewClnSchedules, and instructors viewing their own schedule.
        /// </summary>
        /// <param name="classYear"></param>
        /// <param name="mothraId"></param>
        /// <param name="serviceId"></param>
        /// <param name="weekId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [HttpGet("instructor")]
        public async Task<ActionResult<List<InstructorSchedule>>> GetInstructorSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
                int? weekId, DateTime? startDate, DateTime? endDate, bool active = true)
        {
            if (!clinicalScheduleSecurity.CheckInstructorScheduleParams(mothraId, rotationId, serviceId, weekId, startDate, endDate))
            {
                return ForbidApi();
            }
            var schedule = await clinicalSchedule.GetInstructorSchedule(classYear, mothraId, rotationId, serviceId, weekId, startDate, endDate, active);
            return schedule;
        }
    }
}
