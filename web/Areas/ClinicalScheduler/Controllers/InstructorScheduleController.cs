using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("/clinicalscheduler/instructors/schedule")]
    [Permission(Allow = "SVMSecure.ClnSched.ViewClnSchedules")]
    public class InstructorScheduleController : ApiController
    {
        private readonly VIPERContext _context;

        public InstructorScheduleController(VIPERContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InstructorSchedule>>> GetInstructorSchedules(DateOnly? date, string? mothraId, int? rotationId)
        {
            if (date == null && mothraId == null)
            {
                return ValidationProblem("Date or clinician mothra id is required");
            }
            var sched = _context.InstructorSchedule.AsQueryable();
            if(date != null)
            {
				var dt = date.Value.ToDateTime(TimeOnly.MinValue);
				sched = sched.Where(s => s.DateEnd >= dt)
                    .Where(s => s.DateStart <= dt);
            }
            if(mothraId != null)
            {
                sched = sched.Where(s => s.MothraId == mothraId);
            }
            if(rotationId != null)
            {
                sched = sched.Where(s => s.RotationId == rotationId);
            }

            return await sched
                .OrderBy(s => s.FullName)
                .OrderBy(s => s.RotationName)
                .ToListAsync();
        }
    }
}
