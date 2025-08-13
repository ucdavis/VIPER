using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for handling week data using the vWeek view which contains academic year and week number calculations
    /// Provides access to properly calculated week numbers and academic year associations
    /// </summary>
    public class WeekService : BaseClinicalSchedulerService
    {
        private readonly ILogger<WeekService> _logger;

        public WeekService(ILogger<WeekService> logger, ClinicalSchedulerContext context) : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets weeks for a specific grad year using Entity Framework entities
        /// Returns weeks with proper academic week numbering and year associations
        /// </summary>
        /// <param name="gradYear">The graduation year to get weeks for</param>
        /// <param name="includeExtendedRotation">Whether to include extended rotation weeks</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>List of weeks with proper week numbers and grad year</returns>
        public async Task<List<VWeek>> GetWeeksAsync(int gradYear, bool includeExtendedRotation = true, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.WeekGradYears
                    .AsNoTracking()
                    .Where(wgy => wgy.GradYear == gradYear);

                if (!includeExtendedRotation)
                {
                    query = query.Where(wgy => !wgy.Week.ExtendedRotation);
                }

                // Project to VWeek in the database query for better performance
                var weeks = await query
                    .OrderBy(wgy => wgy.WeekNum)
                    .Select(wgy => new VWeek
                    {
                        WeekId = wgy.WeekId,
                        WeekNum = wgy.WeekNum,
                        DateStart = wgy.Week.DateStart,
                        DateEnd = wgy.Week.DateEnd,
                        ExtendedRotation = wgy.Week.ExtendedRotation,
                        TermCode = wgy.Week.TermCode,
                        StartWeek = wgy.Week.StartWeek,
                        ForcedVacation = false, // This field isn't available in WeekGradYear
                        GradYear = wgy.GradYear,
                        WeekGradYearId = wgy.WeekGradYearId
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} weeks for grad year {GradYear}, includeExtendedRotation: {IncludeExtendedRotation}",
                    weeks.Count, gradYear, includeExtendedRotation);
                return weeks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weeks for grad year {GradYear}", gradYear);
                throw;
            }
        }

        /// <summary>
        /// Gets a specific week by ID using Entity Framework entities
        /// Returns week data with academic year and week number information
        /// </summary>
        /// <param name="weekId">The week ID</param>
        /// <param name="gradYear">Optional grad year filter</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>The week or null if not found</returns>
        public async Task<VWeek?> GetWeekAsync(int weekId, int? gradYear = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.WeekGradYears
                    .AsNoTracking()
                    .Where(wgy => wgy.WeekId == weekId);

                if (gradYear.HasValue)
                {
                    query = query.Where(wgy => wgy.GradYear == gradYear.Value);
                }

                // Project directly to VWeek for better performance
                var week = await query
                    .Select(wgy => new VWeek
                    {
                        WeekId = wgy.WeekId,
                        WeekNum = wgy.WeekNum,
                        DateStart = wgy.Week.DateStart,
                        DateEnd = wgy.Week.DateEnd,
                        ExtendedRotation = wgy.Week.ExtendedRotation,
                        TermCode = wgy.Week.TermCode,
                        StartWeek = wgy.Week.StartWeek,
                        ForcedVacation = false, // This field isn't available in WeekGradYear
                        GradYear = wgy.GradYear,
                        WeekGradYearId = wgy.WeekGradYearId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (week == null)
                {
                    _logger.LogWarning("Week {WeekId} not found for grad year {GradYear}", weekId, gradYear);
                    return null;
                }

                _logger.LogDebug("Retrieved week {WeekId} for grad year {GradYear}, week number {WeekNum}",
                    weekId, week.GradYear, week.WeekNum);
                return week;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving week {WeekId} for grad year {GradYear}", weekId, gradYear);
                throw;
            }
        }

        /// <summary>
        /// Gets the current week based on today's date
        /// Returns the week that contains the current date
        /// </summary>
        /// <param name="gradYear">Optional grad year filter</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>The current week or null if not found</returns>
        public async Task<VWeek?> GetCurrentWeekAsync(int? gradYear = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use DateTime.Today for better clarity and avoid .Date on columns for sargability
                var today = DateTime.Today;
                var query = _context.WeekGradYears
                    .AsNoTracking()
                    .Where(wgy => wgy.Week.DateStart <= today && wgy.Week.DateEnd >= today);

                if (gradYear.HasValue)
                {
                    query = query.Where(wgy => wgy.GradYear == gradYear.Value);
                }

                // Project directly to VWeek for better performance
                var week = await query
                    .Select(wgy => new VWeek
                    {
                        WeekId = wgy.WeekId,
                        WeekNum = wgy.WeekNum,
                        DateStart = wgy.Week.DateStart,
                        DateEnd = wgy.Week.DateEnd,
                        ExtendedRotation = wgy.Week.ExtendedRotation,
                        TermCode = wgy.Week.TermCode,
                        StartWeek = wgy.Week.StartWeek,
                        ForcedVacation = false, // This field isn't available in WeekGradYear
                        GradYear = wgy.GradYear,
                        WeekGradYearId = wgy.WeekGradYearId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (week == null)
                {
                    _logger.LogWarning("No current week found for date {Date} and grad year {GradYear}", today, gradYear);
                    return null;
                }

                _logger.LogInformation("Current week is {WeekNum} for grad year {GradYear}", week.WeekNum, week.GradYear);
                return week;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current week for grad year {GradYear}", gradYear);
                throw;
            }
        }

        /// <summary>
        /// Gets weeks by date range using Entity Framework entities
        /// Returns weeks that overlap with the specified date range
        /// </summary>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>List of weeks that overlap with the date range</returns>
        public async Task<List<VWeek>> GetWeeksByDateRangeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.WeekGradYears
                    .AsNoTracking()
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(wgy => wgy.Week.DateEnd >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(wgy => wgy.Week.DateStart <= endDate.Value);
                }

                // Project to VWeek in the database query with improved ordering for stability
                var weeks = await query
                    .OrderBy(wgy => wgy.Week.DateStart)
                    .ThenBy(wgy => wgy.GradYear)
                    .ThenBy(wgy => wgy.WeekNum)
                    .Select(wgy => new VWeek
                    {
                        WeekId = wgy.WeekId,
                        WeekNum = wgy.WeekNum,
                        DateStart = wgy.Week.DateStart,
                        DateEnd = wgy.Week.DateEnd,
                        ExtendedRotation = wgy.Week.ExtendedRotation,
                        TermCode = wgy.Week.TermCode,
                        StartWeek = wgy.Week.StartWeek,
                        ForcedVacation = false, // This field isn't available in WeekGradYear
                        GradYear = wgy.GradYear,
                        WeekGradYearId = wgy.WeekGradYearId
                    })
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} weeks for date range {StartDate} to {EndDate}",
                    weeks.Count, startDate?.ToString("yyyy-MM-dd") ?? "none", endDate?.ToString("yyyy-MM-dd") ?? "none");
                return weeks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weeks for date range {StartDate} to {EndDate}",
                    startDate?.ToString("yyyy-MM-dd") ?? "none", endDate?.ToString("yyyy-MM-dd") ?? "none");
                throw;
            }
        }
    }
}
