using Hangfire;
using Hangfire.SqlServer;
using NLog;

namespace Viper.Classes.Scheduler
{
    /// <summary>
    /// DI wiring for Hangfire. Gated by <c>Hangfire:Enabled</c>; when the
    /// flag is false (or registration throws) the rest of the web app
    /// continues to start normally.
    /// </summary>
    public static class HangfireExtensions
    {
        /// <summary>
        /// Registers Hangfire services + the background server when
        /// <c>Hangfire:Enabled</c> is true. Wraps registration in a try/catch
        /// so a misconfigured connection string or missing DDL rights logs
        /// the failure and leaves the rest of the app intact (matches the
        /// migration-failure strategy in PLAN-hangfire.md).
        /// </summary>
        public static IServiceCollection AddViperHangfire(
            this IServiceCollection services,
            IConfiguration configuration,
            Logger logger)
        {
            services.Configure<HangfireOptions>(
                configuration.GetSection(HangfireOptions.SectionName));

            var enabled = configuration.GetValue<bool>($"{HangfireOptions.SectionName}:Enabled");
            if (!enabled)
            {
                return services;
            }

            // Defaults to the VIPER connection string but is overridable via
            // ConnectionStrings:Hangfire so the scheduler schema can be moved
            // to a dedicated database without touching app code.
            var connectionString = configuration.GetConnectionString("Hangfire")
                ?? configuration.GetConnectionString("VIPER");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.Error(
                    "Hangfire is enabled but no connection string was found "
                    + "(checked ConnectionStrings:Hangfire and ConnectionStrings:VIPER). "
                    + "Hangfire will be disabled for this process.");
                return services;
            }

            try
            {
                services.AddHangfire(config => config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                    {
                        SchemaName = "HangFire",
                        PrepareSchemaIfNecessary = true,
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true,
                    }));

                services.AddHangfireServer();
            }
            catch (Exception ex)
            {
                // Migration failure strategy: log and continue. The feature
                // flag effectively behaves as "false" for this process.
                logger.Error(
                    ex,
                    "Hangfire registration failed; scheduler is disabled for this process.");
            }

            return services;
        }
    }
}
