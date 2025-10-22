using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.CTS;
using VIPER.Areas.ClinicalScheduler.Utilities;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for managing instructor schedule data retrieval
    /// </summary>
    public class InstructorScheduleService : IInstructorScheduleService
    {
        private readonly VIPERContext _context;
        private readonly ILogger<InstructorScheduleService> _logger;

        public InstructorScheduleService(VIPERContext context, ILogger<InstructorScheduleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get instructor schedule with filtering options
        /// </summary>
        public async Task<List<InstructorSchedule>> GetInstructorScheduleAsync(
            int? classYear = null,
            string? mothraId = null,
            int? rotationId = null,
            int? serviceId = null,
            int? weekId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? active = null)
        {
            try
            {
                _logger.LogDebug("Getting instructor schedule with filters: ClassYear={ClassYear}, MothraId={MothraId}, RotationId={RotationId}, Active={Active}",
                    classYear, LogSanitizer.SanitizeId(mothraId), rotationId, active);

                var query = _context.InstructorSchedules
                    .Include(s => s.Week)
                    .Include(s => s.Service)
                    .Include(s => s.Rotation)
                    .AsQueryable();

                // Apply filters
                if (classYear.HasValue)
                {
                    query = query.Where(s => s.Week.WeekGradYears.Any(gy => gy.GradYear == classYear));
                }

                if (!string.IsNullOrWhiteSpace(mothraId))
                {
                    query = query.Where(s => s.MothraId == mothraId);
                }

                if (rotationId.HasValue)
                {
                    query = query.Where(s => s.RotationId == rotationId);
                }

                if (serviceId.HasValue)
                {
                    query = query.Where(s => s.ServiceId == serviceId);
                }

                if (weekId.HasValue)
                {
                    query = query.Where(s => s.WeekId == weekId);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(s => s.DateEnd >= startDate);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(s => s.DateStart <= endDate);
                }

                if (active.HasValue && active.Value)
                {
                    // Determine active schedules based on recent grad years
                    var cutoff = DateTime.Today.AddMonths(-2);
                    var activeGradYears = await _context.WeekGradYears
                        .Include(w => w.Week)
                        .Where(w => w.Week.DateStart >= cutoff)
                        .Select(w => w.GradYear)
                        .Distinct()
                        .ToListAsync();

                    query = query.Where(s => s.Week.WeekGradYears.Any(w => activeGradYears.Contains(w.GradYear)));
                }

                // Apply ordering
                query = query.OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ThenBy(s => s.DateStart);

                var schedules = await query.ToListAsync();

                _logger.LogInformation("Found {Count} instructor schedules", schedules.Count);
                return schedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving instructor schedules");
                throw new InvalidOperationException("Failed to retrieve instructor schedules", ex);
            }
        }
    }
}
