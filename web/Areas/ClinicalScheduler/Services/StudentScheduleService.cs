using Microsoft.EntityFrameworkCore;
using Viper.Areas.CTS.Models;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for managing student schedule data
    /// </summary>
    public class StudentScheduleService : IStudentScheduleService
    {
        private readonly VIPERContext _context;
        private readonly ILogger<StudentScheduleService> _logger;

        public StudentScheduleService(VIPERContext context, ILogger<StudentScheduleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get student schedule with filtering options
        /// </summary>
        public async Task<List<ClinicalScheduledStudent>> GetStudentScheduleAsync(
            int? classYear = null,
            string? mothraId = null,
            int? rotationId = null,
            int? serviceId = null,
            int? weekId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                _logger.LogDebug("Getting student schedule with filters: ClassYear={ClassYear}, MothraId={MothraId}, RotationId={RotationId}",
                    classYear, mothraId, rotationId);

                var query = _context.StudentSchedules
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

                // Apply ordering
                query = query.OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ThenBy(s => s.DateStart);

                var schedules = await query.ToListAsync();

                // Map to DTO
                var result = schedules.Select(s => new ClinicalScheduledStudent
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

                _logger.LogInformation("Found {Count} student schedules", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student schedules");
                throw new InvalidOperationException("Failed to retrieve student schedules", ex);
            }
        }
    }
}
