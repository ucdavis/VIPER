using Hangfire;
using NLog;
using Viper.Classes.HealthChecks;

namespace Viper.Classes.Scheduler
{
    /// <summary>
    /// DI + pipeline wiring for Hangfire. Gated by <c>Hangfire:Enabled</c>;
    /// when the flag is false the rest of the web app continues to start
    /// normally.
    /// </summary>
    public static class HangfireExtensions
    {
        public const string DashboardPath = "/scheduler/dashboard";
        private const string EnabledKey = "Hangfire:Enabled";

        /// <summary>
        /// Registers Hangfire services + the background server when
        /// <c>Hangfire:Enabled</c> is true. Hangfire's tables live in the
        /// VIPER database under the HangFire schema (auto-migrated on first
        /// server start).
        /// </summary>
        public static IServiceCollection AddViperHangfire(
            this IServiceCollection services,
            IConfiguration configuration,
            Logger logger)
        {
            if (!configuration.GetValue<bool>(EnabledKey))
            {
                return services;
            }

            var connectionString = configuration.GetConnectionString("VIPER");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.Error(
                    "Hangfire is enabled but ConnectionStrings:VIPER is empty. "
                    + "Hangfire will be disabled for this process.");
                return services;
            }

            // Filter is a singleton so a single ILogger instance and any future
            // shared state (counters, etc.) stay process-wide.
            services.AddSingleton<HangfireJobLoggingFilter>();
            services.AddHangfire((sp, config) => config
                .UseSqlServerStorage(connectionString)
                .UseFilter(sp.GetRequiredService<HangfireJobLoggingFilter>()));
            services.AddHangfireServer();

            // Hangfire-specific check piggybacks on the /health/detail "ready"
            // surface stood up in PR 0. Only registered when Hangfire itself is
            // wired so /health/detail doesn't claim a missing subsystem is down.
            services.AddHealthChecks()
                .AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "ready" });

            return services;
        }

        /// <summary>
        /// Mounts the Hangfire dashboard at <see cref="DashboardPath"/> when
        /// Hangfire is actually registered (i.e. <see cref="AddViperHangfire"/>
        /// ran successfully). Unauthenticated visitors hit
        /// <c>RequireAuthorization()</c> first and are redirected to CAS via
        /// the cookie auth challenge; authenticated visitors are gated by
        /// <see cref="HangfireDashboardAuthorizationFilter"/>, which checks
        /// the <c>SVMSecure.CATS.scheduledJobs</c> RAPS permission. Call AFTER
        /// UseRouting / UseAuthentication / UseAuthorization.
        /// </summary>
        public static WebApplication UseViperHangfire(this WebApplication app)
        {
            // Real signal: did AddViperHangfire actually register Hangfire?
            // Covers Enabled=false, missing connection string, and any future
            // short-circuit in one check.
            if (app.Services.GetService<JobStorage>() == null)
            {
                return app;
            }

            app.MapHangfireDashboard(DashboardPath, new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = "VIPER Scheduler",
            }).RequireAuthorization();
            return app;
        }
    }
}
