using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    // DTOs for type safety
    public class ClinicianSummary
    {
        public string MothraId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public string? MailId { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime? LastScheduled { get; set; }
    }

    public class PersonSummary : ClinicianSummary
    {
        public int TotalSchedules { get; set; }
    }

    public class ClinicianYearSummary : ClinicianSummary
    {
        public int Year { get; set; }
        public int ScheduleCount { get; set; }
        public DateTime? FirstScheduled { get; set; }
    }
    /// <summary>
    /// Service for handling person and clinician data from Clinical Scheduler context
    /// Replaces direct AAUD context access by using person data available in Clinical Scheduler views
    /// </summary>
    public class PersonService : BaseClinicalSchedulerService
    {
        private readonly ILogger<PersonService> _logger;

        public PersonService(ILogger<PersonService> logger, ClinicalSchedulerContext context) : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets all active clinicians from Clinical Scheduler instructor schedules
        /// Extracts person data from InstructorSchedule view instead of accessing AAUD
        /// </summary>
        /// <param name="includeHistorical">If true, includes clinicians from older schedules</param>
        /// <param name="sinceDays">Number of days back to look for historical data (default: 730 days / ~2 years)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of unique clinicians with their basic info</returns>
        public async Task<List<ClinicianSummary>> GetCliniciansAsync(bool includeHistorical = true, int sinceDays = 730, CancellationToken cancellationToken = default)
        {
            try
            {
                var cutoffDate = includeHistorical ? DateTime.Now.AddDays(-sinceDays) : DateTime.Now.AddDays(-30);

                _logger.LogInformation("Getting clinicians from Clinical Scheduler InstructorSchedule view (includeHistorical: {IncludeHistorical}, cutoffDate: {CutoffDate})",
                    includeHistorical, cutoffDate.ToString("yyyy-MM-dd"));

                // Get unique clinicians from InstructorSchedule view
                // This contains all person data we need without accessing AAUD
                // Split into two steps to avoid complex LINQ translation issues

                // Step 1: Get all instructor schedules with weeks (simpler query)
                var instructorSchedules = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Include(i => i.Week)
                    .Where(i => i.Week.DateStart >= cutoffDate && !string.IsNullOrEmpty(i.MothraId))
                    .ToListAsync(cancellationToken);

                // Step 2: Group and process in memory (client-side)
                var clinicians = instructorSchedules
                    .GroupBy(i => i.MothraId)
                    .Select(g => new ClinicianSummary
                    {
                        MothraId = g.Key,
                        FullName = g.First().FullName,
                        FirstName = g.First().FirstName,
                        LastName = g.First().LastName,
                        MiddleName = g.First().MiddleName,
                        MailId = g.First().MailId,
                        Source = "InstructorSchedule",
                        LastScheduled = g.Max(x => x.Week.DateEnd)
                    })
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} unique clinicians from Clinical Scheduler data", clinicians.Count);
                return clinicians;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinicians from Clinical Scheduler context");
                throw;
            }
        }

        /// <summary>
        /// Gets person information by MothraId from Clinical Scheduler data
        /// Uses InstructorSchedule view as the source instead of AAUD
        /// </summary>
        /// <param name="mothraId">The MothraId to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Person information or null if not found</returns>
        public async Task<PersonSummary?> GetPersonAsync(string mothraId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mothraId))
                {
                    _logger.LogWarning("GetPersonAsync called with empty MothraId");
                    return null;
                }

                _logger.LogInformation("Getting person data for MothraId: {MothraId} from Clinical Scheduler context", mothraId);

                // Split into two steps to avoid complex LINQ translation issues
                var instructorSchedules = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Include(i => i.Week)
                    .Where(i => i.MothraId == mothraId)
                    .ToListAsync(cancellationToken);

                var person = instructorSchedules
                    .GroupBy(i => i.MothraId)
                    .Select(g => new PersonSummary
                    {
                        MothraId = g.Key,
                        FullName = g.First().FullName,
                        FirstName = g.First().FirstName,
                        LastName = g.First().LastName,
                        MiddleName = g.First().MiddleName,
                        MailId = g.First().MailId,
                        Source = "InstructorSchedule",
                        LastScheduled = g.Max(x => x.Week.DateEnd),
                        TotalSchedules = g.Count()
                    })
                    .FirstOrDefault();

                if (person == null)
                {
                    _logger.LogWarning("Person not found for MothraId: {MothraId} in Clinical Scheduler data", mothraId);
                }
                else
                {
                    _logger.LogInformation("Found person for MothraId: {MothraId} with {TotalSchedules} schedule entries",
                        mothraId, person.TotalSchedules);
                }

                return person;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving person data for MothraId: {MothraId}", mothraId);
                throw;
            }
        }

        /// <summary>
        /// Gets clinicians for a specific year from Clinical Scheduler data
        /// Filters InstructorSchedule entries by year instead of accessing AAUD
        /// </summary>
        /// <param name="year">Year to filter by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clinicians for the specified year</returns>
        public async Task<List<ClinicianYearSummary>> GetCliniciansByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting clinicians for year: {Year} from Clinical Scheduler data", year);

                // Split into two steps to avoid complex LINQ translation issues
                var instructorSchedules = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Include(i => i.Week)
                    .Where(i => i.Week.DateStart.Year == year && !string.IsNullOrEmpty(i.MothraId))
                    .ToListAsync(cancellationToken);

                // Get unique MothraIds for person lookup
                var mothraIds = instructorSchedules.Select(i => i.MothraId).Distinct().ToList();

                // Get person data from vPerson view
                var persons = await _context.Persons
                    .AsNoTracking()
                    .Where(p => mothraIds.Contains(p.IdsMothraId))
                    .ToListAsync(cancellationToken);

                var clinicians = instructorSchedules
                    .GroupBy(i => i.MothraId)
                    .Select(g =>
                    {
                        var person = persons.FirstOrDefault(p => p.IdsMothraId == g.Key);
                        return new ClinicianYearSummary
                        {
                            MothraId = g.Key,
                            FullName = person?.PersonDisplayFullName,
                            FirstName = person?.PersonDisplayFirstName,
                            LastName = person?.PersonDisplayLastName,
                            MiddleName = null, // vPerson doesn't have middle name separately
                            MailId = person?.IdsMailId,
                            Source = "InstructorSchedule+vPerson",
                            Year = year,
                            ScheduleCount = g.Count(),
                            FirstScheduled = g.Min(x => x.Week.DateStart),
                            LastScheduled = g.Max(x => x.Week.DateEnd)
                        };
                    })
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToList();

                _logger.LogInformation("Found {ClinicianCount} clinicians for year {Year} with person names from vPerson view", clinicians.Count, year);
                return clinicians;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinicians for year: {Year}", year);
                throw;
            }
        }

        /// <summary>
        /// Gets all unique MothraIds from Clinical Scheduler data
        /// Useful for validations and lookups
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of unique MothraIds</returns>
        public async Task<List<string>> GetAllMothraIdsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting all unique MothraIds from Clinical Scheduler data");

                var mothraIds = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Where(i => !string.IsNullOrEmpty(i.MothraId))
                    .Select(i => i.MothraId)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Found {Count} unique MothraIds in Clinical Scheduler data", mothraIds.Count);
                return mothraIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unique MothraIds");
                throw;
            }
        }
    }
}