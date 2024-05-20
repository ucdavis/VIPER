using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    /// <summary>
    /// Controller for retrieving student and instructor schedules. Note this should be moved into the clinical scheduler when that app is
    /// created, and the routes adjusted.
    /// </summary>
    [Route("/cts/clinicalschedule/")]
    //most people have this. each function will have more complex permission checking.
    [Permission(Allow = "SVMSecure.ClnSched")]
    public class ClinicalScheduleController : ApiController
    {
        private readonly ClinicalScheduleService clinicalSchedule;
        private readonly ClinicalScheduleSecurityService clinicalScheduleSecurity;
        public ClinicalScheduleController(VIPERContext context, RAPSContext rapsContext)
        {
            clinicalSchedule = new(context, rapsContext);
            clinicalScheduleSecurity = new(rapsContext);
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
        public async Task<ActionResult<List<StudentSchedule>>> GetStudentSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate)
        {
            if (!clinicalScheduleSecurity.CheckStudentScheduleParams(mothraId, rotationId, serviceId, weekId, startDate, endDate))
            {
                return Forbid();
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
                return Forbid();
            }
            var schedule = await clinicalSchedule.GetInstructorSchedule(classYear, mothraId, rotationId, serviceId, weekId, startDate, endDate, active);
            return schedule;
        }
    }
}
