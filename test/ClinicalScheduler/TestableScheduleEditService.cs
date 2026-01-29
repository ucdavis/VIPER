using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Viper.EmailTemplates.Services;
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
            IOptions<EmailSettings> emailSettingsOptions,
            IGradYearService gradYearService,
            IPermissionValidator permissionValidator,
            IEmailTemplateRenderer emailTemplateRenderer)
            : base(context, auditService, logger, emailService, emailNotificationOptions, emailSettingsOptions, gradYearService, permissionValidator, emailTemplateRenderer)
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
