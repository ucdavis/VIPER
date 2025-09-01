namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for graduation year services in the Clinical Scheduler.
    /// Provides methods for retrieving and managing grad year information.
    /// </summary>
    public interface IGradYearService
    {
        /// <summary>
        /// Get the current academic grad year from the Clinical Scheduler database
        /// </summary>
        /// <returns>The current academic grad year</returns>
        Task<int> GetCurrentGradYearAsync();

        /// <summary>
        /// Get the current selection year for the Clinical Scheduler
        /// </summary>
        /// <returns>The current selection year</returns>
        Task<int> GetCurrentSelectionYearAsync();

        /// <summary>
        /// Get all available grad years from the Clinical Scheduler database
        /// </summary>
        /// <param name="publishedOnly">If true, only return years where PublishSchedule is true</param>
        /// <returns>List of available grad years in descending order</returns>
        Task<List<int>> GetAvailableGradYearsAsync(bool publishedOnly = false);
    }
}
