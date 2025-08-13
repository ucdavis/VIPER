using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    /// <summary>
    /// Base controller for Clinical Scheduler providing shared functionality and caching
    /// </summary>
    public abstract class BaseClinicalSchedulerController : ControllerBase
    {
        protected readonly AcademicYearService _academicYearService;
        protected readonly IMemoryCache _cache;
        protected readonly ILogger _logger;

        private const string CURRENT_GRAD_YEAR_CACHE_KEY = "ClinicalScheduler:CurrentGradYear";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

        protected BaseClinicalSchedulerController(
            AcademicYearService academicYearService,
            IMemoryCache cache,
            ILogger logger)
        {
            _academicYearService = academicYearService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets the target year for operations, using provided year or cached current grad year
        /// Caches the current grad year to avoid repeated database calls
        /// </summary>
        /// <param name="year">Optional year parameter from request</param>
        /// <returns>The target academic year to use</returns>
        protected async Task<int> GetTargetYearAsync(int? year)
        {
            if (year.HasValue)
            {
                return year.Value;
            }

            // Try to get from cache first
            if (_cache.TryGetValue(CURRENT_GRAD_YEAR_CACHE_KEY, out int cachedYear))
            {
                _logger.LogDebug("Using cached current grad year: {Year}", cachedYear);
                return cachedYear;
            }

            // Not in cache, fetch from database and cache it
            var currentGradYear = await _academicYearService.GetCurrentGradYearAsync();
            _cache.Set(CURRENT_GRAD_YEAR_CACHE_KEY, currentGradYear, CACHE_DURATION);

            _logger.LogDebug("Fetched and cached current grad year: {Year}", currentGradYear);
            return currentGradYear;
        }

        /// <summary>
        /// Gets the current grad year with caching
        /// </summary>
        /// <returns>The current academic grad year</returns>
        protected async Task<int> GetCurrentGradYearAsync()
        {
            return await GetTargetYearAsync(null);
        }

        /// <summary>
        /// Clears the current grad year cache - useful for testing or when academic year changes
        /// </summary>
        protected void ClearCurrentGradYearCache()
        {
            _cache.Remove(CURRENT_GRAD_YEAR_CACHE_KEY);
            _logger.LogInformation("Cleared current grad year cache");
        }
    }
}