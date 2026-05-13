using System.Reflection;
using Hangfire;
using Hangfire.Console;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.Server;
using NLog;
using Viper.Areas.Scheduler.Services;
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
        private const string AutoScheduleKey = "Hangfire:AutoSchedule";
        private const string DashboardAppPathKey = "Hangfire:DashboardAppPath";

        /// <summary>
        /// Registers Hangfire services + the background server when
        /// <c>Hangfire:Enabled</c> is true. Hangfire's tables live in the
        /// VIPER database under the HangFire schema (auto-migrated on first
        /// server start). When <c>Hangfire:AutoSchedule</c> is false (e.g.
        /// local dev), the worker still runs and the dashboard still mounts,
        /// but recurring jobs are registered with <c>Cron.Never</c> so cron
        /// never fires; jobs are visible in the dashboard and can be invoked
        /// via "Trigger now" or <c>BackgroundJob.Enqueue</c>.
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
                .UseSqlServerStorage(connectionString, new Hangfire.SqlServer.SqlServerStorageOptions
                {
                    SchemaName = "HangFire",
                })
                .UseFilter(sp.GetRequiredService<HangfireJobLoggingFilter>())
                // Hangfire.Console: per-job execution logs in the dashboard
                // (jobs accept PerformContext and call context.WriteLine).
                .UseConsole()
                // Hangfire.Heartbeat: dashboard tab that renders server
                // metrics. The recorder is added below as a background process
                // on the worker; both must run for the graph to populate.
                .UseHeartbeatPage(checkInterval: TimeSpan.FromSeconds(30)));

            // ProcessMonitor (from Hangfire.Heartbeat) records the CPU/RAM/
            // uptime metrics rendered by UseHeartbeatPage. Registering it as
            // an IBackgroundProcess in DI lets AddHangfireServer pick it up
            // alongside the default workers without us having to pass storage
            // explicitly (the overload that takes additionalProcesses also
            // requires a non-null JobStorage).
            services.AddSingleton<IBackgroundProcess>(_ => new ProcessMonitor(checkInterval: TimeSpan.FromSeconds(30)));
            services.AddHangfireServer();

            // Hangfire-specific check piggybacks on the /health/detail "ready"
            // surface stood up in PR 0. Only registered when Hangfire itself is
            // wired so /health/detail doesn't claim a missing subsystem is down.
            services.AddHealthChecks()
                .AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "ready" });

            ScheduledJobDiscovery.RegisterScheduledJobs(
                services,
                new[] { Assembly.GetExecutingAssembly() });

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
            if (app.Services.GetService<JobStorage>() == null)
            {
                return app;
            }

            // The dashboard's "Back to site" link uses AppPath verbatim;
            // we have no UsePathBase middleware so the /2/ deployment prefix
            // (TEST/PROD) must come from config.
            var dashboardAppPath = app.Configuration.GetValue<string>(DashboardAppPathKey) ?? "/Computing";
            app.MapHangfireDashboard(DashboardPath, new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = "VIPER Scheduler",
                AppPath = dashboardAppPath,
            }).RequireAuthorization();

            // When AutoSchedule is off (dev), register every recurring job
            // with Cron.Never so the dashboard still shows them and operators
            // can fire them via "Trigger now" or BackgroundJob.Enqueue, but
            // nothing fires on its own.
            var autoSchedule = app.Configuration.GetValue<bool?>(AutoScheduleKey) ?? true;
            var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
            var registry = app.Services.GetRequiredService<IScheduledJobRegistry>();
            ScheduledJobDiscovery.RegisterRecurringJobs(recurringJobManager, registry.JobsById.Values, autoSchedule);
            return app;
        }
    }
}
