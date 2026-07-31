using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Viper.Classes.Utilities
{
    public static class TransactionHelper
    {
        /// <summary>
        /// Runs <paramref name="operation"/> inside a database transaction at the given isolation
        /// level, committing on success and rolling back on any exception.
        /// </summary>
        public static async Task<T> ExecuteInTransactionAsync<T>(
            DatabaseFacade database,
            Func<CancellationToken, Task<T>> operation,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            await using var transaction = await database.BeginTransactionAsync(isolationLevel, cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
