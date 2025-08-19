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

        public GradYearService(ILogger<GradYearService> logger, ClinicalSchedulerContext context) : base(context)
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
            try
            {
                // Get the default grad year from the Status table
                var defaultStatus = await _context.Statuses
                    .AsNoTracking()
                    .Where(s => s.DefaultGradYear)
                    .FirstOrDefaultAsync();

                if (defaultStatus != null)
                {
                    _logger.LogInformation("Retrieved current grad year from Status table: {GradYear}", defaultStatus.GradYear);
                    return defaultStatus.GradYear;
                }
                else
                {
                    var calculatedGradYear = CalculateCurrentGradYear();
                    _logger.LogWarning("No default grad year found in Status table, using calculated academic grad year: {GradYear}", calculatedGradYear);
                    return calculatedGradYear;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current grad year from Status table");

                // Fallback to calculated academic grad year
                var fallbackYear = CalculateCurrentGradYear();
                _logger.LogWarning("Using fallback calculated academic grad year: {Year}", fallbackYear);
                return fallbackYear;
            }
        }

        /// <summary>
        /// Gets the default selection year from the Status table
        /// Returns the year marked as the default selection year for student selection processes
        /// </summary>
        /// <returns>The current selection year</returns>
        public async Task<int> GetCurrentSelectionYearAsync()
        {
            // Current implementation returns calculated grad year since selection year data
            // is not yet integrated with the Clinical Scheduler database schema
            var currentYear = CalculateCurrentGradYear();

            _logger.LogWarning("Using calculated grad year for selection year: {SelectionYear}. Selection year configuration not yet implemented.", currentYear);
            return await Task.FromResult(currentYear);
        }

        /// <summary>
        /// Calculates the current graduation year based on UC Davis School of Veterinary Medicine calendar
        ///
        /// UC Davis Veterinary School uses a semester system:
        ///
        /// Years 1-3: Two semesters per year
        /// - Fall semester: starts in August
        /// - Spring semester: ends in late May
        ///
        /// Year 4: Three semesters (year-round clinical training)
        /// - Summer semester: begins in June/July
        /// - Fall semester: continues from August
        /// - Spring semester: ends with graduation in late May
        ///
        /// This Clinical Scheduler is primarily for Year 4 students doing clinical rotations
        /// under clinician supervision. The grad year represents when students will graduate.
        ///
        /// Calculation logic:
        /// - August-December: Students are in grad year that graduates next May
        /// - January-July: Students are in grad year that graduates this May
        ///
        /// Examples:
        /// - August 2024: grad year is 2025 (will graduate May 2025)
        /// - January 2025: grad year is 2025 (will graduate May 2025)
        ///
        /// Note: Year 4 summer semester starts before August, but this calculation
        /// focuses on the graduation year rather than semester transitions.
        /// </summary>
        /// <returns>The calculated graduation year</returns>
        private static int CalculateCurrentGradYear()
        {
            var now = DateTime.Now;
            var currentCalendarYear = now.Year;

            // Graduation year calculation (UC Davis veterinary school specific)
            // Focus is on when students will graduate, not semester transitions
            if (now.Month >= 8) // August or later
            {
                // We're in the grad year that will graduate in the following calendar year
                return currentCalendarYear + 1;
            }
            else
            {
                // We're in January-July, students will graduate this May
                return currentCalendarYear;
            }
        }

        /// <summary>
        /// Gets all available grad years from the Status table
        /// Returns all configured graduation years in the system
        /// </summary>
        /// <param name="publishedOnly">If true, only returns years where PublishSchedule is true</param>
        /// <returns>List of available grad years in descending order</returns>
        public async Task<List<int>> GetAvailableGradYearsAsync(bool publishedOnly = false)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available grad years from Status table");

                // Fallback to current and recent academic grad years
                var currentYear = CalculateCurrentGradYear();
                var fallbackYears = new List<int> { currentYear, currentYear - 1, currentYear - 2, currentYear - 3 };

                _logger.LogWarning("Using fallback calculated academic grad years: {Years}", string.Join(", ", fallbackYears));
                return fallbackYears;
            }
        }
    }
}