namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for person-related services in the Clinical Scheduler.
    /// Provides methods for retrieving clinician and person information.
    /// </summary>
    public interface IPersonService
    {
        /// <summary>
        /// Get all clinicians based on their instruction schedule history
        /// </summary>
        /// <param name="includeHistorical">Include historical clinicians beyond the recent time frame</param>
        /// <param name="sinceDays">Number of days back to look for recent schedules (default: 730 days / 2 years)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clinicians with their summary information</returns>
        Task<List<ClinicianSummary>> GetCliniciansAsync(bool includeHistorical = true, int sinceDays = 730, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get clinicians filtered by a specific year
        /// </summary>
        /// <param name="year">The year to filter clinicians by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clinicians for the specified year</returns>
        Task<List<ClinicianYearSummary>> GetCliniciansByYearAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get detailed information for a specific person by their MothraId
        /// </summary>
        /// <param name="mothraId">The person's MothraId</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Person information or null if not found</returns>
        Task<PersonSummary?> GetPersonAsync(string mothraId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all unique MothraIds from the instructor schedules
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of unique MothraIds</returns>
        Task<List<string>> GetAllMothraIdsAsync(CancellationToken cancellationToken = default);
    }
}