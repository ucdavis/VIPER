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
            // Use ExecuteSqlRaw to execute and get result with proper connection string
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT TOP (1) GradYear FROM [ClinicalScheduler].[dbo].[Status] WHERE DefaultGradYear = 1 ORDER BY GradYear DESC";

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("No default grad year found in ClinicalScheduler Status table");
            }

            var defaultGradYear = Convert.ToInt32(result);
            _logger.LogInformation("Retrieved default grad year from ClinicalScheduler database: {GradYear}", defaultGradYear);
            return defaultGradYear;
        }

        /// <summary>
        /// Gets the default selection year from the Status table
        /// Returns the year marked as the default selection year for student selection processes
        /// </summary>
        /// <returns>The current selection year</returns>
        public async Task<int> GetCurrentSelectionYearAsync()
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT TOP (1) GradYear FROM [ClinicalScheduler].[dbo].[Status] WHERE DefaultSelectionYear = 1 ORDER BY GradYear DESC";

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException("No default selection year found in ClinicalScheduler Status table");
            }

            var defaultSelectionYear = Convert.ToInt32(result);
            _logger.LogInformation("Retrieved default selection year from ClinicalScheduler database: {SelectionYear}", defaultSelectionYear);
            return defaultSelectionYear;
        }

        /// <summary>
        /// Gets all available grad years from the Status table
        /// Returns all configured graduation years in the system
        /// </summary>
        /// <param name="publishedOnly">If true, only returns years where PublishSchedule is true</param>
        /// <returns>List of available grad years in descending order</returns>
        public async Task<List<int>> GetAvailableGradYearsAsync(bool publishedOnly = false)
        {
            var sql = publishedOnly
                ? "SELECT DISTINCT GradYear FROM [ClinicalScheduler].[dbo].[Status] WHERE PublishSchedule = 1 ORDER BY GradYear DESC"
                : "SELECT DISTINCT GradYear FROM [ClinicalScheduler].[dbo].[Status] ORDER BY GradYear DESC";

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            var years = new List<int>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                years.Add(reader.GetInt32(0));
            }

            _logger.LogInformation("Retrieved {Count} available grad years from ClinicalScheduler database", years.Count);
            return years;
        }
    }
}