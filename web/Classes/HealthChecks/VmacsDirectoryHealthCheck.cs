using Microsoft.Extensions.Diagnostics.HealthChecks;
using Viper.Areas.Directory.Services;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Probes the VMACS directory service query endpoint (/trust/query.xml) that
    /// VMACSService.Search depends on - not just the VMACS host - so a failure here
    /// pinpoints that endpoint being down rather than a bug in VIPER.
    ///
    /// It issues the same authenticated query as Search for a fixed probe login and
    /// treats any HTTP 200 that parses as a VMACS query response as Healthy; an empty
    /// result still proves endpoint + auth + response shape. Severity encodes fault:
    /// an outage (unreachable / timeout / 5xx / unparseable) is Degraded - directory
    /// enrichment is optional and the rest of VIPER works - while our-side
    /// misconfiguration (Vmacs:BaseUrl unset, or the token rejected) is Unhealthy.
    /// </summary>
    public class VmacsDirectoryHealthCheck : IHealthCheck
    {
        // A stable probe login. It need not resolve to a record - a valid token
        // returns HTTP 200 with an empty <query> for an unknown id - so the check
        // stays green across environments with different datasets (QA is sparse).
        private const string ProbeLoginId = "_testdvm";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _baseUrl;
        private readonly string? _authToken;
        private readonly bool _healthyWhenTokenMissing;
        private readonly TimeSpan _timeout;

        public VmacsDirectoryHealthCheck(
            IHttpClientFactory httpClientFactory,
            string? baseUrl,
            string? authToken,
            bool healthyWhenTokenMissing = false,
            TimeSpan? timeout = null)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = baseUrl;
            _authToken = authToken;
            _healthyWhenTokenMissing = healthyWhenTokenMissing;
            _timeout = timeout ?? TimeSpan.FromSeconds(10);
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (!VMACSService.IsValidBaseUrl(_baseUrl))
            {
                return HealthCheckResult.Unhealthy(
                    "Vmacs:BaseUrl is not configured or is not a valid absolute http(s) URL.");
            }
            if (string.IsNullOrWhiteSpace(_authToken))
            {
                return _healthyWhenTokenMissing
                    ? HealthCheckResult.Healthy("VMACS auth token not configured (skipped).")
                    : HealthCheckResult.Unhealthy("Credentials:VmacsAuthToken is not configured.");
            }

            var requestUri = _baseUrl.TrimEnd('/') + VMACSService.BuildSearchPath(ProbeLoginId, _authToken);

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = _timeout;

            try
            {
                using var response = await client.GetAsync(requestUri, cancellationToken);
                var code = (int)response.StatusCode;

                if (code is 401 or 403)
                {
                    return HealthCheckResult.Unhealthy(
                        $"VMACS directory endpoint rejected the request (HTTP {code}) - check Credentials:VmacsAuthToken.");
                }
                if (!response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Degraded($"VMACS directory endpoint returned HTTP {code}.");
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    return VMACSService.Deserialize(body) != null
                        ? HealthCheckResult.Healthy($"VMACS directory endpoint reachable (HTTP {code}).")
                        : HealthCheckResult.Degraded(
                            "VMACS directory endpoint returned a response that did not parse.");
                }
                catch (InvalidOperationException)
                {
                    return HealthCheckResult.Degraded(
                        "VMACS directory endpoint returned a response that was not valid XML.");
                }
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Degraded($"VMACS directory endpoint unreachable: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                return cancellationToken.IsCancellationRequested
                    ? HealthCheckResult.Degraded("VMACS directory endpoint probe cancelled.")
                    : HealthCheckResult.Degraded("VMACS directory endpoint timed out.");
            }
        }
    }
}
