using Hangfire.Server;
using Viper.Classes.Utilities;

namespace Viper.Classes.Scheduler
{
    /// <summary>
    /// Hangfire server filter that wraps every job execution in a structured
    /// logging scope (jobId / recurringJobId / triggerSource) and emits
    /// start/complete/error log entries. All user-influenced values are run
    /// through <see cref="LogSanitizer"/> before being logged.
    /// </summary>
    public sealed class HangfireJobLoggingFilter : IServerFilter
    {
        private const string ScopeKey = "__HangfireLoggingScope";

        private readonly ILogger<HangfireJobLoggingFilter> _logger;

        public HangfireJobLoggingFilter(ILogger<HangfireJobLoggingFilter> logger)
        {
            _logger = logger;
        }

        public void OnPerforming(PerformingContext context)
        {
            var jobId = LogSanitizer.SanitizeId(context.BackgroundJob.Id);
            var recurringJobId = LogSanitizer.SanitizeString(context.GetJobParameter<string>("RecurringJobId"));
            var triggerSource = string.IsNullOrEmpty(recurringJobId) ? "Manual" : "Scheduled";

            // Stash the scope on context.Items because OnPerforming and OnPerformed
            // are separate calls; a `using` block here would dispose too early.
            var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["jobId"] = jobId ?? string.Empty,
                ["recurringJobId"] = recurringJobId ?? string.Empty,
                ["triggerSource"] = triggerSource
            });

            if (scope != null)
            {
                context.Items[ScopeKey] = scope;
            }

            // Log only argument metadata (count). Logging values risks leaking
            // secrets/PII even after control-char sanitization, so the value
            // strings are intentionally not emitted.
            var argCount = context.BackgroundJob.Job.Args?.Count ?? 0;

            _logger.LogInformation(
                "Hangfire job starting: {JobType}.{JobMethod} (argCount={ArgCount})",
                LogSanitizer.SanitizeString(context.BackgroundJob.Job.Type.FullName),
                LogSanitizer.SanitizeString(context.BackgroundJob.Job.Method.Name),
                argCount);
        }

        public void OnPerformed(PerformedContext context)
        {
            // Dispose the scope stashed by OnPerforming via `using` so the
            // structured logging fields stay attached for the log lines below
            // and are released at method exit.
            using var scope = context.Items.TryGetValue(ScopeKey, out var stashed)
                ? stashed as IDisposable
                : null;
            try
            {
                if (context.Exception != null)
                {
                    _logger.LogError(context.Exception, "Hangfire job threw exception");
                }
                else
                {
                    _logger.LogInformation("Hangfire job completed");
                }
            }
            finally
            {
                context.Items.Remove(ScopeKey);
            }
        }
    }
}
