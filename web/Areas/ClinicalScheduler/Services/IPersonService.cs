namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for person-related services in the Clinical Scheduler.
    /// Provides methods for retrieving clinician and person information.
    /// </summary>
    public interface IPersonService
    {

        /// <summary>
        /// Get clinicians filtered by a specific year
        /// </summary>
        /// <param name="year">The year to filter clinicians by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clinicians for the specified year</returns>
        Task<List<ClinicianYearSummary>> GetCliniciansByYearAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get clinicians for a range of grad years
        /// </summary>
        /// <param name="startGradYear">Starting grad year (inclusive)</param>
        /// <param name="endGradYear">Ending grad year (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of clinicians who were scheduled during the specified grad year range</returns>
        Task<List<ClinicianSummary>> GetCliniciansByGradYearRangeAsync(int startGradYear, int endGradYear, CancellationToken cancellationToken = default);

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
