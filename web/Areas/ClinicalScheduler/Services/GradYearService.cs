using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for managing graduation year operations within the clinical scheduler
    /// Provides access to available graduation years and current year calculations
    /// </summary>
    public class GradYearService : BaseClinicalSchedulerService, IGradYearService
    {
        private readonly ILogger<GradYearService> _logger;

        public GradYearService(
            ILogger<GradYearService> logger,
            ClinicalSchedulerContext context) : base(context)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the default academic grad year from the Status table
        /// Returns the year marked as the default graduation year in the database
        /// </summary>
        /// <returns>The current academic grad year</returns>
        public async Task<int> GetCurrentGradYearAsync()
        {
            // Get the default grad year from the Status table
            var defaultStatus = await _context.Statuses
                .AsNoTracking()
                .Where(s => s.DefaultGradYear)
                .FirstOrDefaultAsync();

            if (defaultStatus == null)
            {
                _logger.LogError("No default grad year found in Status table. A default grad year must be configured.");
                throw new InvalidOperationException("No default grad year is configured in the Status table. Please configure a default grad year.");
            }

            _logger.LogInformation("Retrieved current grad year from Status table: {GradYear}", defaultStatus.GradYear);
            return defaultStatus.GradYear;
        }

        /// <summary>
        /// Gets the default selection year from the Status table
        /// Returns the year marked as the default selection year for student selection processes
        /// </summary>
        /// <returns>The current selection year</returns>
        public async Task<int> GetCurrentSelectionYearAsync()
        {
            // Get the default selection year from the Status table
            var defaultStatus = await _context.Statuses
                .AsNoTracking()
                .Where(s => s.DefaultSelectionYear)
                .FirstOrDefaultAsync();

            if (defaultStatus == null)
            {
                _logger.LogError("No default selection year found in Status table. A default selection year must be configured.");
                throw new InvalidOperationException("No default selection year is configured in the Status table. Please configure a default selection year.");
            }

            _logger.LogInformation("Retrieved current selection year from Status table: {SelectionYear}", defaultStatus.GradYear);
            return defaultStatus.GradYear;
        }

        /// <summary>
        /// Gets all available grad years from the Status table
        /// Returns all configured graduation years in the system
        /// </summary>
        /// <param name="publishedOnly">If true, only returns years where PublishSchedule is true</param>
        /// <returns>List of available grad years in descending order</returns>
        public async Task<List<int>> GetAvailableGradYearsAsync(bool publishedOnly = false)
        {
            // Query the Status table to get configured grad years
            var query = _context.Statuses.AsNoTracking();

            if (publishedOnly)
            {
                query = query.Where(s => s.PublishSchedule);
            }

            var configuredYears = await query
                .Select(s => s.GradYear)
                .OrderByDescending(year => year)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} configured grad years from Status table: {Years}",
                configuredYears.Count, string.Join(", ", configuredYears));

            // Filter out years that don't have week data
            // Check which years actually have weeks in the WeekGradYears table
            var yearsWithWeekData = await _context.WeekGradYears
                .AsNoTracking()
                .Select(wgy => wgy.GradYear)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("Found {Count} years with week data: {Years}",
                yearsWithWeekData.Count, string.Join(", ", yearsWithWeekData.OrderByDescending(y => y)));

            // Only include years that have both configuration AND week data
            var availableYears = configuredYears
                .Where(year => yearsWithWeekData.Contains(year))
                .OrderByDescending(year => year)
                .ToList();

            _logger.LogInformation("Filtered to {Count} available grad years with week data: {Years}",
                availableYears.Count, string.Join(", ", availableYears));

            return availableYears;
        }

    }
}
