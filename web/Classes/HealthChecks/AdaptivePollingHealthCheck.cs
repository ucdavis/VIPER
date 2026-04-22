using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Throttles an inner health check by caching its last result for a
    /// status-dependent duration: Healthy results are reused for longer, while
    /// Unhealthy/Degraded results refresh on a tighter cycle so recovery is
    /// noticed quickly. When a cached result is returned, the original probe
    /// timestamp is appended to the description so operators can tell how
    /// stale the reading is.
    /// </summary>
    public class AdaptivePollingHealthCheck : IHealthCheck
    {
        private readonly IHealthCheck _inner;
        private readonly TimeSpan _healthyCacheDuration;
        private readonly TimeSpan _unhealthyCacheDuration;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private DateTime _lastCheckTimeUtc;
        private HealthCheckResult? _lastResult;

        public AdaptivePollingHealthCheck(
            IHealthCheck inner,
            TimeSpan healthyCacheDuration,
            TimeSpan unhealthyCacheDuration)
        {
            _inner = inner;
            _healthyCacheDuration = healthyCacheDuration;
            _unhealthyCacheDuration = unhealthyCacheDuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_lastResult.HasValue)
                {
                    var elapsed = DateTime.UtcNow - _lastCheckTimeUtc;
                    var cacheDuration = _lastResult.Value.Status == HealthStatus.Healthy
                        ? _healthyCacheDuration
                        : _unhealthyCacheDuration;

                    if (elapsed < cacheDuration)
                    {
                        return AppendTimestamp(_lastResult.Value, _lastCheckTimeUtc.ToLocalTime());
                    }
                }

                var result = await _inner.CheckHealthAsync(context, cancellationToken);
                _lastResult = result;
                _lastCheckTimeUtc = DateTime.UtcNow;
                return result;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static HealthCheckResult AppendTimestamp(HealthCheckResult result, DateTime lastCheckedAt)
        {
            var stamp = $"Last checked: {lastCheckedAt:MMM d, h:mm tt}";
            var description = string.IsNullOrWhiteSpace(result.Description)
                ? stamp
                : $"{result.Description}\n{stamp}";
            return new HealthCheckResult(
                result.Status,
                description,
                result.Exception,
                result.Data);
        }
    }
}
