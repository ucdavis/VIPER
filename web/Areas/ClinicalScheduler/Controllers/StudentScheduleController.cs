using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("/clinicalscheduler/students/schedule")]
    [Permission(Allow = "SVMSecure.ClnSched.ViewStdSchedules")]
    public class StudentScheduleController : ApiController
    {
        private readonly ClinicalSchedulerContext _context;

        public StudentScheduleController(ClinicalSchedulerContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentSchedule>>> GetStudentSchedules(DateOnly? date, string? mothraId, int? rotationId)
        {
            //check required arguments: at least date or student
            //for future, add student + grad year or rotation + grad year?
            if (date == null && mothraId == null)
            {
                return ValidationProblem("Date or student mothra id is required");
            }

            var sched = _context.StudentSchedules
                .Include(s => s.Week)
                .AsQueryable();
            if (date != null)
            {
                var dt = date.Value.ToDateTime(TimeOnly.MinValue);
                sched = sched.Where(s => s.Week.DateEnd >= dt)
                    .Where(s => s.Week.DateStart <= dt);
            }
            if (mothraId != null)
            {
                // StudentSchedule uses MailId, not MothraId
                sched = sched.Where(s => s.MailId == mothraId);
            }
            if (rotationId != null)
            {
                sched = sched.Where(s => s.RotationId == rotationId);
            }

            return await sched
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
    }
}
