using System.Reflection;
using Hangfire;
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

            // Hangfire shares the VIPER DB so the marker table (queried via
            // VIPERContext) and Hangfire's own tables stay in one database.
            // A separate connection string would split EF queries from DDL
            // and break pause/resume; revisit when there's a scheduler DbContext.
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
                    // Pin the schema explicitly so case-sensitive collations and
                    // future Hangfire defaults can't drift from the [HangFire]
                    // schema the marker table colocates against.
                    SchemaName = "HangFire",
                })
                .UseFilter(sp.GetRequiredService<HangfireJobLoggingFilter>()));
            services.AddHangfireServer();

            // Hangfire-specific check piggybacks on the /health/detail "ready"
            // surface stood up in PR 0. Only registered when Hangfire itself is
            // wired so /health/detail doesn't claim a missing subsystem is down.
            services.AddHealthChecks()
                .AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "ready" });

            // Scheduler API surface — registered here (not via Scrutor) so
            // controller activation only succeeds when Hangfire is wired,
            // avoiding 500s on /api/scheduler/jobs* with the flag off.
            services.AddScoped<ISchedulerJobsService, SchedulerJobsService>();

            // Reconciler is needed by both the startup hosted service and the
            // hourly recurring job; resolved as a transient because each entry
            // point creates its own scope for the underlying service.
            services.AddTransient<SchedulerJobReconciler>();
            services.AddHostedService<ReconcilerStartupHostedService>();

            // Discover [ScheduledJob] implementations so the registry is
            // available to ISchedulerJobsService (reconciler) and the post-
            // mount registrar can wire each one into Hangfire.
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
            // Real signal: did AddViperHangfire actually register Hangfire?
            // Covers Enabled=false, missing connection string, and any future
            // short-circuit in one check.
            if (app.Services.GetService<JobStorage>() == null)
            {
                return app;
            }

            // Idempotent DDL: ensure the marker table exists so pause/resume
            // doesn't fail on first run. Resolving JobStorage above triggered
            // Hangfire's bootstrap, which guarantees the [HangFire] schema is
            // present before we attach our marker table to it.
            var nlogger = LogManager.GetCurrentClassLogger();
            var connectionString = app.Configuration.GetConnectionString("VIPER");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                SchedulerSchemaInitializer.EnsureSchedulerJobStateTable(connectionString, nlogger);
            }

            app.MapHangfireDashboard(DashboardPath, new DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
                DashboardTitle = "VIPER Scheduler",
            }).RequireAuthorization();

            // When AutoSchedule is off (dev), register every recurring job
            // with Cron.Never so the dashboard still shows them and operators
            // can fire them via "Trigger now" or BackgroundJob.Enqueue, but
            // nothing fires on its own.
            var autoSchedule = app.Configuration.GetValue<bool?>(AutoScheduleKey) ?? true;
            var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
            if (autoSchedule)
            {
                // Register the hourly reconciler. AddOrUpdate is idempotent so
                // the call is safe across restarts; running it here (after
                // Hangfire is mounted) guarantees the storage layer is ready.
                SchedulerJobReconciler.RegisterRecurring(recurringJobManager);
            }

            // Same idempotent pass for every [ScheduledJob]-declared job. The
            // reconciler also re-registers lost ones, but doing it on startup
            // means a fresh deploy doesn't have to wait an hour to converge.
            // When AutoSchedule is off, RegisterRecurringJobs swaps each
            // declared cron for Cron.Never so jobs stay visible without firing.
            var registry = app.Services.GetRequiredService<IScheduledJobRegistry>();
            ScheduledJobDiscovery.RegisterRecurringJobs(recurringJobManager, registry.JobsById.Values, autoSchedule);
            return app;
        }
    }
}
