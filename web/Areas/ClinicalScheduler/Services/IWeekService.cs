using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for week-related services in the Clinical Scheduler.
    /// Provides methods for retrieving week information and schedule data.
    /// </summary>
    public interface IWeekService
    {
        /// <summary>
        /// Get weeks for a specific graduation year using the vWeek view with proper week number calculation
        /// </summary>
        /// <param name="gradYear">The graduation year to get weeks for</param>
        /// <param name="includeExtendedRotation">Include weeks marked as extended rotation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of weeks with proper week numbers and grad year</returns>
        Task<List<VWeek>> GetWeeksAsync(int gradYear, bool includeExtendedRotation = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific week by its ID
        /// </summary>
        /// <param name="weekId">The week ID to retrieve</param>
        /// <param name="gradYear">Optional graduation year for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The week or null if not found</returns>
        Task<VWeek?> GetWeekAsync(int weekId, int? gradYear = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the current week based on today's date
        /// </summary>
        /// <param name="gradYear">Optional graduation year for context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The current week or null if not found</returns>
        Task<VWeek?> GetCurrentWeekAsync(int? gradYear = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get weeks that fall within a specific date range
        /// </summary>
        /// <param name="startDate">Start date of the range (optional)</param>
        /// <param name="endDate">End date of the range (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of weeks that overlap with the date range</returns>
        Task<List<VWeek>> GetWeeksByDateRangeAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    }
}
