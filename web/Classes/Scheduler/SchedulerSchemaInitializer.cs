using Microsoft.Data.SqlClient;
using NLog;

namespace Viper.Classes.Scheduler
{
    /// <summary>
    /// Creates the [HangFire].[SchedulerJobState] table on startup if it does
    /// not already exist. This project does not use EF migrations, and the
    /// table colocates with Hangfire's own tables in the HangFire schema; the
    /// schema itself is auto-created by Hangfire on first server start, so we
    /// only need to add our own marker table once it exists.
    /// </summary>
    public static class SchedulerSchemaInitializer
    {
        private const string EnsureTableSql = @"
IF NOT EXISTS (
    SELECT 1 FROM sys.tables t
    INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'HangFire' AND t.name = 'SchedulerJobState')
BEGIN
    CREATE TABLE [HangFire].[SchedulerJobState] (
        [RecurringJobId]  NVARCHAR(200)  NOT NULL,
        [Cron]            VARCHAR(100)   NOT NULL,
        [Queue]           VARCHAR(100)   NOT NULL,
        [TimeZoneId]      VARCHAR(100)   NOT NULL,
        [JobTypeName]     VARCHAR(500)   NOT NULL,
        [SerializedArgs]  NVARCHAR(MAX)  NOT NULL,
        [PausedAt]        DATETIME2      NOT NULL,
        [PausedBy]        VARCHAR(100)   NOT NULL,
        [RowVersion]      ROWVERSION     NOT NULL,
        CONSTRAINT [PK_HangFire_SchedulerJobState] PRIMARY KEY CLUSTERED ([RecurringJobId])
    );
END";

        /// <summary>
        /// Runs the idempotent DDL against the supplied connection string.
        /// Call AFTER Hangfire's own bootstrap (which creates the HangFire
        /// schema). Errors are logged but do not throw — a failure here means
        /// pause/resume APIs return 5xx, while the rest of Hangfire keeps
        /// running.
        /// </summary>
        public static void EnsureSchedulerJobStateTable(string connectionString, Logger logger)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = EnsureTableSql;
                command.ExecuteNonQuery();
            }
            // Swallow expected storage failures so a transient DB blip during
            // startup doesn't crash the host; pause/resume APIs will fail
            // loudly later if the table is genuinely missing.
            catch (Exception ex) when (ex is SqlException
                or InvalidOperationException
                or TimeoutException)
            {
                logger.Error(ex, "Failed to ensure HangFire.SchedulerJobState table exists");
            }
        }
    }
}
