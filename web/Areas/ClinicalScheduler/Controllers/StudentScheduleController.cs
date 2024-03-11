using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("/clinicalscheduler/students/schedule")]
    [Permission(Allow = "SVMSecure.ClnSched.ViewStdSchedules")]
    public class StudentScheduleController : ApiController
    {
        private readonly VIPERContext _context;

        public StudentScheduleController(VIPERContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentSchedule>>> GetStudentSchedules(DateOnly? date, string? mothraId, int? rotationId)
        {
            //check required arguments: at least date or student
            //for future, add student + grad year or rotation + grad year?
            if (date == null && mothraId == null )
            {
                return ValidationProblem("Date or student mothra id is required");
            }

            var sched = _context.StudentSchedule.AsQueryable();
            if (date != null)
            {
                var dt = date.Value.ToDateTime(TimeOnly.MinValue);
                sched = sched.Where(s => s.DateEnd >= dt)
                    .Where(s => s.DateStart <= dt);
            }
            if (mothraId != null)
            {
                sched = sched.Where(s => s.MothraId == mothraId);
            }
            if (rotationId != null)
            {
                sched = sched.Where(s => s.RotationId == rotationId);
            }

            return await sched
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }
    }
}
