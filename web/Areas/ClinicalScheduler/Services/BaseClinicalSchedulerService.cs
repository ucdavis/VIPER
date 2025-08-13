using Viper.Classes.SQLContext;
using Microsoft.EntityFrameworkCore;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Base service class for Clinical Scheduler services providing common functionality
    /// </summary>
    public abstract class BaseClinicalSchedulerService
    {
        protected readonly ClinicalSchedulerContext _context;

        protected BaseClinicalSchedulerService(ClinicalSchedulerContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the connection string from the database context (for VIPER database)
        /// </summary>
        /// <returns>The connection string for VIPER database access</returns>
        protected string GetConnectionString()
        {
            // Use the same connection string as Entity Framework context
            var connectionString = _context.Database.GetDbConnection().ConnectionString;
            return connectionString ?? throw new InvalidOperationException("Connection string not found in context");
        }

        /// <summary>
        /// Gets the connection string for the ClinicalScheduler database
        /// This is used for accessing views like vWeek and Status that are in a separate database
        /// </summary>
        /// <returns>The connection string for ClinicalScheduler database access</returns>
        protected string GetClinicalSchedulerConnectionString()
        {
            // Try to get a specific ClinicalScheduler connection string first
            var clinicalSchedulerConnectionString = HttpHelper.Settings?["ConnectionStrings:ClinicalScheduler"];
            if (!string.IsNullOrEmpty(clinicalSchedulerConnectionString))
            {
                return clinicalSchedulerConnectionString;
            }

            // Fall back to the VIPER connection string if no specific ClinicalScheduler connection string is configured
            // This assumes the ClinicalScheduler database is accessible via the same connection
            return GetConnectionString();
        }
    }
}
