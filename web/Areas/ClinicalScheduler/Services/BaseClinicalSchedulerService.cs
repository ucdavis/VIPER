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
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the ClinicalScheduler database connection string for raw SQL operations
        /// Use sparingly - prefer Entity Framework through _context when possible
        /// </summary>
        /// <returns>ClinicalScheduler database connection string</returns>
        protected string GetConnectionString()
        {
            return _context.Database.GetConnectionString() ?? throw new InvalidOperationException("ClinicalScheduler connection string not configured");
        }
    }
}
