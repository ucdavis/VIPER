using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.ClinicalScheduler.Services
{
    // DTOs for type safety
    public class ClinicianSummary
    {
        public string MothraId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class ClinicianYearSummary : ClinicianSummary
    {
        public int Year { get; set; }
    }
    /// <summary>
    /// Service for handling person and clinician data from Clinical Scheduler context
    /// Replaces direct AAUD context access by using person data available in Clinical Scheduler views
    /// </summary>
    public class PersonService : BaseClinicalSchedulerService, IPersonService
    {
        private readonly ILogger<PersonService> _logger;
        private readonly AAUDContext _aaudContext;

        public PersonService(ILogger<PersonService> logger, ClinicalSchedulerContext context, AAUDContext aaudContext) : base(context)
        {
            _logger = logger;
            _aaudContext = aaudContext;
        }

        /// <summary>
        /// Gets person information by MothraId from Clinical Scheduler data
        /// Uses InstructorSchedule view as the source instead of AAUD
        /// </summary>
        /// <param name="mothraId">The MothraId to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Person information or null if not found</returns>
        public async Task<ClinicianSummary?> GetPersonAsync(string mothraId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mothraId))
                {
                    _logger.LogWarning("GetPersonAsync called with empty MothraId");
                    return null;
                }

                _logger.LogInformation("Getting person data for MothraId: {MothraId} from Clinical Scheduler context", LogSanitizer.SanitizeId(mothraId));

                // Query the vPerson view directly - much more efficient than joining through InstructorSchedules
                var personData = await _context.Persons
                    .AsNoTracking()
                    .Where(p => p.IdsMothraId == mothraId)
                    .FirstOrDefaultAsync(cancellationToken);

                ClinicianSummary? person = null;
                if (personData != null)
                {
                    person = new ClinicianSummary
                    {
                        MothraId = mothraId,
                        FullName = personData.PersonDisplayFullName ?? "Unknown",
                        FirstName = personData.PersonDisplayFirstName ?? "Unknown",
                        LastName = personData.PersonDisplayLastName ?? "Unknown"
                    };
                }

                if (person == null)
                {
                    _logger.LogWarning("Person not found for MothraId: {MothraId} in Clinical Scheduler data", LogSanitizer.SanitizeId(mothraId));
                }
                else
                {
                    _logger.LogInformation("Found person for MothraId: {MothraId}", LogSanitizer.SanitizeId(mothraId));
                }

                return person;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving person data for MothraId: {MothraId}", LogSanitizer.SanitizeId(mothraId));
                throw new InvalidOperationException($"Failed to retrieve person data for MothraId {LogSanitizer.SanitizeId(mothraId)}", ex);
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
                            Year = year
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
                throw new InvalidOperationException($"Failed to retrieve clinicians for grad year {year}. Check database connectivity and view permissions.", ex);
            }
        }

        /// <summary>
        /// Gets clinicians for a range of grad years (more efficient than day-based filtering)
        /// </summary>
        /// <param name="startGradYear">Starting grad year (inclusive)</param>
        /// <param name="endGradYear">Ending grad year (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clinicians who were scheduled during the specified grad year range</returns>
        public async Task<List<ClinicianSummary>> GetCliniciansByGradYearRangeAsync(int startGradYear, int endGradYear, CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Get unique MothraIds for the grad year range
                var mothraIds = await _context.InstructorSchedules
                    .AsNoTracking()
                    .Where(i => i.Week.WeekGradYears.Any(wgy =>
                        wgy.GradYear >= startGradYear &&
                        wgy.GradYear <= endGradYear))
                    .Where(i => !string.IsNullOrEmpty(i.MothraId))
                    .Select(i => i.MothraId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (mothraIds.Count == 0)
                {
                    return [];
                }

                // Step 2: Fetch person details
                var persons = await _context.Persons
                    .AsNoTracking()
                    .Where(p => mothraIds.Contains(p.IdsMothraId))
                    .ToListAsync(cancellationToken);

                // Step 3: Build clinician summaries with dictionary lookup
                var personById = persons.ToDictionary(p => p.IdsMothraId, p => p);
                var clinicians = mothraIds
                    .Select(mothraId =>
                    {
                        personById.TryGetValue(mothraId, out var person);
                        return new ClinicianSummary
                        {
                            MothraId = mothraId,
                            FullName = person?.PersonDisplayFullName ?? $"Clinician {mothraId}",
                            FirstName = person?.PersonDisplayFirstName ?? "",
                            LastName = person?.PersonDisplayLastName ?? ""
                        };
                    })
                    .OrderBy(c => c.LastName ?? "")
                    .ThenBy(c => c.FirstName ?? "")
                    .ToList();

                return clinicians;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Query timeout retrieving clinicians for grad year range {StartYear}-{EndYear}", LogSanitizer.SanitizeYear(startGradYear), LogSanitizer.SanitizeYear(endGradYear));
                throw new TimeoutException($"Timed out retrieving clinicians for grad year range {LogSanitizer.SanitizeYear(startGradYear)}-{LogSanitizer.SanitizeYear(endGradYear)}.", ex);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Database error retrieving clinicians for grad year range {StartYear}-{EndYear}", LogSanitizer.SanitizeYear(startGradYear), LogSanitizer.SanitizeYear(endGradYear));
                throw new InvalidOperationException($"Database error retrieving clinicians for grad year range {LogSanitizer.SanitizeYear(startGradYear)}-{LogSanitizer.SanitizeYear(endGradYear)}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinicians for grad year range {StartYear}-{EndYear}", LogSanitizer.SanitizeYear(startGradYear), LogSanitizer.SanitizeYear(endGradYear));
                throw new InvalidOperationException($"Failed to retrieve clinicians for grad year range {LogSanitizer.SanitizeYear(startGradYear)}-{LogSanitizer.SanitizeYear(endGradYear)}", ex);
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
                throw new InvalidOperationException("Failed to retrieve unique MothraIds from database", ex);
            }
        }

        /// <summary>
        /// Get all active employee affiliates from AAUD database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active employee affiliates</returns>
        public async Task<List<ClinicianSummary>> GetAllActiveEmployeeAffiliatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Fetching all active employee affiliates from AAUD database");

                var allAffiliates = await _aaudContext.AaudUsers
                    .AsNoTracking()
                    .Where(u => !string.IsNullOrEmpty(u.MothraId) &&
                               !string.IsNullOrEmpty(u.EmployeeId) &&
                               (u.CurrentEmployee || u.FutureEmployee))
                    .Select(u => new ClinicianSummary
                    {
                        MothraId = u.MothraId,
                        FullName = u.DisplayFullName,
                        FirstName = u.DisplayFirstName,
                        LastName = u.DisplayLastName
                    })
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Found {Count} active employee affiliates from AAUD", allAffiliates.Count);
                return allAffiliates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active employee affiliates from AAUD");
                throw new InvalidOperationException("Failed to retrieve active employee affiliates from AAUD database", ex);
            }
        }

        /// <summary>
        /// Get clinician info from AAUD context as fallback
        /// </summary>
        /// <param name="mothraId">The MothraId to look up</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Clinician info or null if not found</returns>
        public async Task<ClinicianSummary?> GetClinicianFromAaudAsync(string mothraId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mothraId))
                {
                    _logger.LogWarning("GetClinicianFromAaudAsync called with empty MothraId");
                    return null;
                }

                _logger.LogDebug("Getting clinician data for MothraId: {MothraId} from AAUD context", LogSanitizer.SanitizeId(mothraId));

                var clinician = await _aaudContext.VwVmthClinicians
                    .AsNoTracking()
                    .Where(c => c.IdsMothraid == mothraId)
                    .Select(c => new ClinicianSummary
                    {
                        MothraId = mothraId,
                        FullName = ResolveClinicianName(c.FullName, c.PersonDisplayFirstName, c.PersonDisplayLastName, mothraId),
                        FirstName = c.PersonDisplayFirstName ?? "",
                        LastName = c.PersonDisplayLastName ?? ""
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (clinician == null)
                {
                    _logger.LogWarning("Clinician not found for MothraId: {MothraId} in AAUD data", LogSanitizer.SanitizeId(mothraId));
                }
                else
                {
                    _logger.LogDebug("Found clinician for MothraId: {MothraId} in AAUD data", LogSanitizer.SanitizeId(mothraId));
                }

                return clinician;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clinician data for MothraId: {MothraId} from AAUD", LogSanitizer.SanitizeId(mothraId));
                throw new InvalidOperationException($"Failed to retrieve clinician data for MothraId {LogSanitizer.SanitizeId(mothraId)} from AAUD", ex);
            }
        }

        /// <summary>
        /// Helper method to resolve clinician full name with fallbacks
        /// </summary>
        /// <param name="fullName">Primary full name from data source</param>
        /// <param name="firstName">First name to use as fallback</param>
        /// <param name="lastName">Last name to use as fallback</param>
        /// <param name="mothraId">MothraId to use as final fallback</param>
        /// <returns>Resolved full name</returns>
        private static string ResolveClinicianName(string? fullName, string? firstName, string? lastName, string mothraId)
        {
            var first = firstName?.Trim();
            var last = lastName?.Trim();

            if (!string.IsNullOrWhiteSpace(last) && !string.IsNullOrWhiteSpace(first))
                return $"{last}, {first}";

            // Fallback to fullName (which is "First Last" format) or mothraId
            return !string.IsNullOrWhiteSpace(fullName) ? fullName : $"Clinician {mothraId}";
        }
    }
}
