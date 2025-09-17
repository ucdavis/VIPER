using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.Services;

namespace Viper.test.ClinicalScheduler
{
    /// <summary>
    /// Testable version of ScheduleEditService that bypasses transaction handling for unit tests
    /// </summary>
    internal class TestableScheduleEditService : ScheduleEditService
    {
        public TestableScheduleEditService(
            ClinicalSchedulerContext context,
            IScheduleAuditService auditService,
            ILogger<ScheduleEditService> logger,
            IEmailService emailService,
            IOptions<EmailNotificationSettings> emailNotificationOptions,
            IGradYearService gradYearService,
            IPermissionValidator permissionValidator)
            : base(context, auditService, logger, emailService, emailNotificationOptions, gradYearService, permissionValidator)
        {
        }

        /// <summary>
        /// Override transaction handling to execute operations directly without transactions for testing
        /// </summary>
        protected override async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            System.Data.IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            // Execute the operation directly without transaction for testing
            return await operation(cancellationToken);
        }

        /// <summary>
        /// Override transaction handling to execute operations directly without transactions for testing
        /// </summary>
        protected override async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            // Execute the operation directly without transaction for testing
            return await operation(cancellationToken);
        }
    }
}
