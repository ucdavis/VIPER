using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Reports the state of the Hangfire job system: storage reachability,
    /// registered server count, and heartbeat freshness. Only registered when
    /// Hangfire is enabled, so JobStorage is guaranteed to be in DI.
    /// </summary>
    public class HangfireHealthCheck : IHealthCheck
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        // Hangfire's default heartbeat interval is 30s; 2 minutes covers a few
        // missed beats before we call a server stale.
        private static readonly TimeSpan StaleHeartbeatThreshold = TimeSpan.FromMinutes(2);

        private readonly JobStorage _storage;

        public HangfireHealthCheck(JobStorage storage)
        {
            _storage = storage;
        }

        /// <summary>
        /// Probes Hangfire storage and server heartbeats, reporting Healthy,
        /// Degraded (no servers), or Unhealthy (storage error or stale heartbeats).
        /// </summary>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            IMonitoringApi monitoringApi;
            StatisticsDto stats;
            IList<ServerDto> servers;
            try
            {
                monitoringApi = _storage.GetMonitoringApi();
                stats = monitoringApi.GetStatistics();
                servers = monitoringApi.Servers();
            }
            // Hangfire wraps SqlException/TimeoutException/InvalidOperationException
            // and more inside its storage layer; letting any escape would crash the
            // health-response writer, so a broad catch is the right call here.
            catch (Exception ex)
            {
                _logger.Error(ex, "Hangfire health check failed to query storage");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Hangfire storage unreachable.", exception: ex));
            }

            var data = new Dictionary<string, object>
            {
                ["servers"] = stats.Servers,
                ["enqueued"] = stats.Enqueued,
                ["scheduled"] = stats.Scheduled,
                ["processing"] = stats.Processing,
                ["failed"] = stats.Failed,
                ["recurring"] = stats.Recurring,
            };

            if (servers.Count == 0)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Hangfire storage reachable but no servers registered.", data: data));
            }

            var now = DateTime.UtcNow;
            // Treat a null Heartbeat as maximally stale so a server that never
            // reported in still trips the stale-heartbeat path.
            var ages = servers
                .Select(s => s.Heartbeat.HasValue ? now - s.Heartbeat.Value : TimeSpan.MaxValue)
                .ToList();
            var oldestAge = ages.Max();

            if (ages.All(age => age > StaleHeartbeatThreshold))
            {
                var formatted = oldestAge == TimeSpan.MaxValue
                    ? "never"
                    : oldestAge.ToString("hh\\:mm\\:ss");
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Hangfire servers registered but heartbeats are stale (oldest: {formatted}).",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Hangfire OK: {servers.Count} server(s).", data: data));
        }
    }
}
