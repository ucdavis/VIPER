using Microsoft.EntityFrameworkCore;
using System;
using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;

namespace Viper.Areas.CTS.Services
{
    public class ClinicalScheduleService
    {
        private readonly VIPERContext context;
        private readonly RAPSContext rapsContext;
        public ClinicalScheduleService(VIPERContext context, RAPSContext rapsContext)
        {
            this.context = context;
            this.rapsContext = rapsContext;
        }

        public async Task<List<ClinicalScheduledStudent>> GetStudentSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate)
        {
            var sched = context.StudentSchedules
                .Include(s => s.Week)
                .Include(s => s.Service)
                .Include(s => s.Rotation)
                .AsQueryable();
            if (classYear != null)
            {
                sched = sched.Where(s => s.Week.WeekGradYears.Any(gy => gy.GradYear == classYear));
            }
            if (mothraId != null)
            {
                sched = sched.Where(s => s.MothraId == mothraId);
            }
            if (rotationId != null)
            {
                sched = sched.Where(s => s.RotationId == rotationId);
            }
            if (serviceId != null)
            {
                sched = sched.Where(s => s.ServiceId == serviceId);
            }
            if (weekId != null)
            {
                sched = sched.Where(s => s.WeekId == weekId);
            }
            if (startDate != null)
            {
                sched = sched.Where(s => s.DateEnd >= startDate);
            }
            if (endDate != null)
            {
                sched = sched.Where(s => s.DateStart <= endDate);
            }

            sched = sched.OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ThenBy(s => s.DateStart);

            var s = (await sched.ToListAsync());

            return s.Select(s => new ClinicalScheduledStudent()
            {
                PersonId = s.PersonId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                MiddleName = s.MiddleName,
                FullName = s.FullName,
                MothraId = s.MothraId,
                MailId = s.MailId,
                DateStart = s.DateStart,
                DateEnd = s.DateEnd,
                WeekId = s.WeekId,
                RotationId = s.RotationId,
                RotationName = s.RotationName,
                ServiceId = s.ServiceId,
                ServiceName = s.ServiceName,
                CrseNumb = s.CrseNumb,
                SubjCode = s.SubjCode
            }).ToList();
        }

        public async Task<List<InstructorSchedule>> GetInstructorSchedule(int? classYear, string? mothraId, int? rotationId, int? serviceId,
            int? weekId, DateTime? startDate, DateTime? endDate, bool? active)
        {
            var sched = context.InstructorSchedules
                .Include(s => s.Week)
                .Include(s => s.Service)
                .Include(s => s.Rotation)
                .AsQueryable();
            if (classYear != null)
            {
                sched = sched.Where(s => s.Week.WeekGradYears.Any(gy => gy.GradYear == classYear));
            }
            if (mothraId != null)
            {
                sched = sched.Where(s => s.MothraId == mothraId);
            }
            if (rotationId != null)
            {
                sched = sched.Where(s => s.RotationId == rotationId);
            }
            if (serviceId != null)
            {
                sched = sched.Where(s => s.ServiceId == serviceId);
            }
            if (weekId != null)
            {
                sched = sched.Where(s => s.WeekId == weekId);
            }
            if (startDate != null)
            {
                sched = sched.Where(s => s.DateEnd >= startDate);
            }
            if (endDate != null)
            {
                sched = sched.Where(s => s.DateStart <= endDate);
            }
            if (active != null)
            {
                //placeholder - how to determine the active schedule(s)
                var cutoff = DateTime.Today.AddMonths(-2);
                var gradYears = await context.WeekGradYears
                    .Include(w => w.Week)
                    .Where(w => w.Week.DateStart >= cutoff)
                    .Select(w => w.GradYear).Distinct()
                    .ToListAsync();
                sched = sched.Where(s => s.Week.WeekGradYears.Any(w => gradYears.Contains(w.GradYear)));
            }

            sched = sched.OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ThenBy(s => s.DateStart);

            var s = await sched.ToListAsync();
            return s;
        }

    }
}
