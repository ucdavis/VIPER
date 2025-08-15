using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for handling academic year calculations and retrieving current grad year from database
    /// Provides access to the current academic year settings for clinical scheduler operations
    /// </summary>
    public class AcademicYearService : BaseClinicalSchedulerService
    {
        private readonly ILogger<AcademicYearService> _logger;

        public AcademicYearService(ILogger<AcademicYearService> logger, ClinicalSchedulerContext context) : base(context)
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
                // Query the Status table using Entity Framework to get the default grad year
                var defaultStatus = await _context.Statuses
                    .AsNoTracking()
                    .Where(s => s.DefaultGradYear)
                    .FirstOrDefaultAsync();

                if (defaultStatus != null)
                {
                    _logger.LogInformation("Retrieved default grad year from Status table: {GradYear}", defaultStatus.GradYear);
                    return defaultStatus.GradYear;
                }
                else
                {
                    _logger.LogWarning("No default grad year found in Status table, using current calendar year");
                    return DateTime.Now.Year;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving default grad year from Status table");

                // Fallback to current calendar year
                var fallbackYear = DateTime.Now.Year;
                _logger.LogWarning("Using fallback current year: {Year}", fallbackYear);
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
            // Current implementation returns calendar year since selection year data
            // is not yet integrated with the Clinical Scheduler database schema
            var currentYear = DateTime.Now.Year;

            _logger.LogWarning("Using calendar year for selection year: {SelectionYear}. Selection year configuration not yet implemented.", currentYear);
            return await Task.FromResult(currentYear);
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
                // Query the weekGradYear table to get actual available grad years
                var availableYears = await _context.WeekGradYears
                    .AsNoTracking()
                    .Select(wgy => wgy.GradYear)
                    .Distinct()
                    .OrderByDescending(year => year)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} available grad years from weekGradYear table: {Years}",
                    availableYears.Count, string.Join(", ", availableYears));

                return availableYears;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available grad years from weekGradYear table");

                // Fallback to current and recent years
                var currentYear = DateTime.Now.Year;
                var fallbackYears = new List<int> { currentYear, currentYear - 1, currentYear - 2, currentYear - 3 };

                _logger.LogWarning("Using fallback hardcoded grad years: {Years}", string.Join(", ", fallbackYears));
                return fallbackYears;
            }
        }
    }
}