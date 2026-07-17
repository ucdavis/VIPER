using System.Data;
using Microsoft.Extensions.Logging;
using Viper.Areas.CMS.Services;
using Viper.Classes.SQLContext;

namespace Viper.test.CMS
{
    /// <summary>
    /// Testable version of CmsFileService that bypasses transaction handling for unit tests
    /// (the EF in-memory provider used in these tests doesn't support transactions).
    /// </summary>
    internal class TestableCmsFileService : CmsFileService
    {
        public TestableCmsFileService(
            VIPERContext context,
            AAUDContext aaudContext,
            ICmsFileStorageService storage,
            ICmsFileEncryptionService encryption,
            ICmsFileAuditService audit,
            IUserHelper userHelper,
            ILogger<CmsFileService> logger)
            : base(context, aaudContext, storage, encryption, audit, userHelper, logger)
        {
        }

        protected override async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            // Execute the operation directly without transaction for testing
            return await operation(cancellationToken);
        }
    }
}
