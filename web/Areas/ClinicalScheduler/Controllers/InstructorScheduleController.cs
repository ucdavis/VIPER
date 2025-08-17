using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    [Route("/clinicalscheduler/instructors/schedule")]
    [Permission(Allow = "SVMSecure.ClnSched.ViewClnSchedules")]
    public class InstructorScheduleController : ApiController
    {
        private readonly ClinicalSchedulerContext _context;

        public InstructorScheduleController(ClinicalSchedulerContext context)
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
            var sched = _context.InstructorSchedules
                .Include(s => s.Week)
                .Include(s => s.Person)
                .Include(s => s.Rotation)
                .AsQueryable();
            if (date != null)
            {
                var dt = date.Value.ToDateTime(TimeOnly.MinValue);
                sched = sched.Where(s => s.Week.DateEnd >= dt)
                    .Where(s => s.Week.DateStart <= dt);
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
                .OrderBy(s => s.Person != null ? s.Person.PersonDisplayLastName : s.MothraId)
                .ThenBy(s => s.Rotation.Name)
                .ToListAsync();
        }
    }
}
