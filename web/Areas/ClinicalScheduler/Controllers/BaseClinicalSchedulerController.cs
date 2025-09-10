using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    /// <summary>
    /// Base controller for Clinical Scheduler providing shared functionality
    /// Now inherits from ApiController to get standardized exception handling, response formatting, and filters
    /// </summary>
    public abstract class BaseClinicalSchedulerController : ApiController
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
        /// Stores contextual information for exception handling by ApiExceptionFilter
        /// Call this before throwing exceptions to provide additional context for logging and debugging
        /// </summary>
        /// <param name="contextInfo">Dictionary of key-value pairs providing context about the operation that failed</param>
        protected void SetExceptionContext(Dictionary<string, object> contextInfo)
        {
            HttpContext.Items["ExceptionContext"] = contextInfo;
        }

        /// <summary>
        /// Convenience method to set single context property for exception handling
        /// </summary>
        /// <param name="key">The context property name</param>
        /// <param name="value">The context property value</param>
        protected void SetExceptionContext(string key, object value)
        {
            SetExceptionContext(new Dictionary<string, object> { [key] = value });
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
