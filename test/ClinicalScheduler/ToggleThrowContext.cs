using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// In-memory <see cref="ClinicalSchedulerContext"/> that can be told to throw a specific exception
    /// from SaveChanges, so tests can exercise the database-failure handling in
    /// <see cref="Viper.Areas.ClinicalScheduler.Services.ScheduleEditService"/>. Leave
    /// <see cref="SaveException"/> null for normal behavior (e.g. seeding test data).
    /// </summary>
    internal sealed class ToggleThrowContext : ClinicalSchedulerContext
    {
        public ToggleThrowContext(DbContextOptions<ClinicalSchedulerContext> options) : base(options)
        {
        }

        /// <summary>When set, the next SaveChanges call throws this exception instead of persisting.</summary>
        public Exception? SaveException { get; set; }

        public override int SaveChanges()
        {
            if (TakePendingException() is { } ex)
            {
                throw ex;
            }
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (TakePendingException() is { } ex)
            {
                return Task.FromException<int>(ex);
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        // One-shot: consume the pending exception so only the next save fails, matching the
        // documented behavior. Subsequent saves persist normally.
        private Exception? TakePendingException()
        {
            var ex = SaveException;
            SaveException = null;
            return ex;
        }
    }
}
