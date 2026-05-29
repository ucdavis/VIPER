using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Probes an HTTP(S) endpoint with a short timeout. Treats any non-5xx
    /// response (including 4xx) as Healthy - we're checking reachability, not
    /// whether the endpoint accepts our request. 5xx or network failure =
    /// Unhealthy.
    /// </summary>
    public class HttpEndpointHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _url;
        private readonly string _displayName;
        private readonly TimeSpan _timeout;

        public HttpEndpointHealthCheck(
            IHttpClientFactory httpClientFactory,
            string url,
            string displayName,
            TimeSpan? timeout = null)
        {
            _httpClientFactory = httpClientFactory;
            _url = url;
            _displayName = displayName;
            _timeout = timeout ?? TimeSpan.FromSeconds(5);
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = _timeout;

            try
            {
                using var response = await client.GetAsync(
                    _url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                var code = (int)response.StatusCode;
                return code < 500
                    ? HealthCheckResult.Healthy($"{_displayName} reachable (HTTP {code}).")
                    : HealthCheckResult.Unhealthy($"{_displayName} returned HTTP {code}.");
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"{_displayName} unreachable: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                // HttpClient.Timeout cancellation and external cancellation (app
                // shutdown, RequestAborted) both surface as TaskCanceledException.
                return cancellationToken.IsCancellationRequested
                    ? HealthCheckResult.Unhealthy($"{_displayName} probe cancelled.")
                    : HealthCheckResult.Unhealthy($"{_displayName} timed out.");
            }
        }
    }
}
