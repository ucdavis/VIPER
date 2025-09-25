using Microsoft.EntityFrameworkCore;
using Viper.Areas.ClinicalScheduler.Extensions;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for handling week data using the vWeek view which contains grad year and week number calculations
    /// Provides access to properly calculated week numbers and grad year associations
    /// </summary>
    public class WeekService : BaseClinicalSchedulerService, IWeekService
    {
        private readonly ILogger<WeekService> _logger;

        public WeekService(ILogger<WeekService> logger, ClinicalSchedulerContext context) : base(context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets weeks for a specific grad year using the vWeek view directly
        /// Returns weeks with proper academic week numbering and year associations
        /// </summary>
        /// <param name="gradYear">The graduation year to get weeks for</param>
        /// <param name="includeExtendedRotation">Whether to include extended rotation weeks</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>List of weeks with proper week numbers and grad year</returns>
        public async Task<List<WeekDto>> GetWeeksAsync(int gradYear, bool includeExtendedRotation = true, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.VWeeks
                    .AsNoTracking()
                    .Where(w => w.GradYear == gradYear);

                if (!includeExtendedRotation)
                {
                    query = query.Where(w => !w.ExtendedRotation);
                }

                // Query the view directly - no deduplication needed since we're filtering by grad year
                var weeks = await query
                    .OrderBy(w => w.WeekNum)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} weeks for grad year {GradYear}, includeExtendedRotation: {IncludeExtendedRotation}",
                    weeks.Count, gradYear, includeExtendedRotation);
                return weeks.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weeks for grad year {GradYear}: {ErrorMessage}", gradYear, ex.Message);
                throw new InvalidOperationException($"Failed to retrieve weeks for graduation year {gradYear}", ex);
            }
        }

        /// <summary>
        /// Gets a specific week by ID using the vWeek view directly
        /// Returns week data with grad year and week number information
        /// </summary>
        /// <param name="weekId">The week ID</param>
        /// <param name="gradYear">Optional grad year filter</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>The week or null if not found</returns>
        public async Task<WeekDto?> GetWeekAsync(int weekId, int? gradYear = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.VWeeks
                    .AsNoTracking()
                    .Where(w => w.WeekId == weekId);

                if (gradYear.HasValue)
                {
                    query = query.Where(w => w.GradYear == gradYear.Value);
                }

                // Query the view directly
                var week = await query.FirstOrDefaultAsync(cancellationToken);

                if (week == null)
                {
                    _logger.LogWarning("Week {WeekId} not found for grad year {GradYear}", weekId, gradYear);
                    return null;
                }

                _logger.LogDebug("Retrieved week {WeekId} for grad year {GradYear}, week number {WeekNum}",
                    weekId, week.GradYear, week.WeekNum);
                return week.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving week {WeekId} for grad year {GradYear}", weekId, gradYear);
                throw new InvalidOperationException($"Failed to retrieve week {weekId} for graduation year {gradYear}", ex);
            }
        }

        /// <summary>
        /// Gets the current week based on today's date
        /// Returns the week that contains the current date
        /// </summary>
        /// <param name="gradYear">Optional grad year filter</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>The current week or null if not found</returns>
        public async Task<WeekDto?> GetCurrentWeekAsync(int? gradYear = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use DateTime.Today for better clarity
                var today = DateTime.Today;
                var query = _context.VWeeks
                    .AsNoTracking()
                    .Where(w => w.DateStart <= today && w.DateEnd >= today);

                if (gradYear.HasValue)
                {
                    query = query.Where(w => w.GradYear == gradYear.Value);
                }

                // Query the view directly
                var week = await query.FirstOrDefaultAsync(cancellationToken);

                if (week == null)
                {
                    _logger.LogWarning("No current week found for date {Date} and grad year {GradYear}", today, gradYear);
                    return null;
                }

                _logger.LogInformation("Current week is {WeekNum} for grad year {GradYear}", week.WeekNum, week.GradYear);
                return week.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current week for grad year {GradYear}", gradYear);
                throw new InvalidOperationException($"Failed to retrieve current week for graduation year {gradYear}", ex);
            }
        }

        /// <summary>
        /// Gets weeks by date range using the vWeek view directly
        /// Returns weeks that overlap with the specified date range
        /// </summary>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>List of weeks that overlap with the date range</returns>
        public async Task<List<WeekDto>> GetWeeksByDateRangeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.VWeeks
                    .AsNoTracking()
                    .AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(w => w.DateEnd >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(w => w.DateStart <= endDate.Value);
                }

                // Query the view directly with improved ordering for stability
                var weeks = await query
                    .OrderBy(w => w.DateStart)
                    .ThenBy(w => w.GradYear)
                    .ThenBy(w => w.WeekNum)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} weeks for date range {StartDate} to {EndDate}",
                    weeks.Count, startDate?.ToString("yyyy-MM-dd") ?? "none", endDate?.ToString("yyyy-MM-dd") ?? "none");
                return weeks.ToDto();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weeks for date range {StartDate} to {EndDate}",
                    startDate?.ToString("yyyy-MM-dd") ?? "none", endDate?.ToString("yyyy-MM-dd") ?? "none");
                throw new InvalidOperationException($"Failed to retrieve weeks for date range {startDate?.ToString("yyyy-MM-dd") ?? "none"} to {endDate?.ToString("yyyy-MM-dd") ?? "none"}", ex);
            }
        }
    }
}
