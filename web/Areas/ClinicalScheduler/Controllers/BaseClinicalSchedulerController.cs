using Microsoft.AspNetCore.Mvc;
using Viper.Areas.ClinicalScheduler.Services;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    /// <summary>
    /// Base controller for Clinical Scheduler providing shared functionality
    /// </summary>
    public abstract class BaseClinicalSchedulerController : ControllerBase
    {
        protected readonly IGradYearService _gradYearService;
        protected readonly ILogger<BaseClinicalSchedulerController> _logger;

        protected BaseClinicalSchedulerController(
            IGradYearService gradYearService,
            ILogger<BaseClinicalSchedulerController> logger)
        {
            _gradYearService = gradYearService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the target year for operations, using provided year or current grad year from database
        /// </summary>
        /// <param name="year">Optional year parameter from request</param>
        /// <returns>The target grad year to use</returns>
        protected async Task<int> GetTargetYearAsync(int? year)
        {
            if (year.HasValue)
            {
                return year.Value;
            }

            // Fetch current grad year directly from database - it's a simple query
            var currentGradYear = await _gradYearService.GetCurrentGradYearAsync();
            _logger.LogDebug("Using current grad year: {Year}", currentGradYear);
            return currentGradYear;
        }

        /// <summary>
        /// Gets the current grad year from database
        /// </summary>
        /// <returns>The current grad year</returns>
        protected async Task<int> GetCurrentGradYearAsync()
        {
            return await _gradYearService.GetCurrentGradYearAsync();
        }

        /// <summary>
        /// Handles exceptions and returns a standardized error response with structured logging
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="message">The error message to return to the client</param>
        /// <param name="contextProperty">Optional context property name for logging</param>
        /// <param name="contextValue">Optional context value for logging</param>
        /// <returns>A standardized 500 Internal Server Error response</returns>
        protected ObjectResult HandleException(Exception ex, string message, string? contextProperty = null, object? contextValue = null)
        {
            var correlationId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(contextProperty) && contextValue != null)
            {
                _logger.LogError(ex, "{Message}. CorrelationId: {CorrelationId}. {ContextProperty}: {ContextValue}",
                    message, correlationId, contextProperty, contextValue);
            }
            else
            {
                _logger.LogError(ex, "{Message}. CorrelationId: {CorrelationId}", message, correlationId);
            }

            return StatusCode(500, new
            {
                error = message,
                correlationId
            });
        }

        /// <summary>
        /// Helper method to normalize semester names from TermCodeService
        /// </summary>
        /// <param name="termCode">The term code to convert</param>
        /// <returns>Normalized semester name</returns>
        protected static string GetNormalizedSemesterName(int termCode)
        {
            var semesterName = Areas.Curriculum.Services.TermCodeService.GetTermCodeDescription(termCode);
            return semesterName.StartsWith("Unknown Term") ? "Unknown Semester" : semesterName;
        }

    }
}
