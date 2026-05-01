namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Wired into HealthChecksUI's API-endpoint HttpClient via
    /// UseApiEndpointDelegatingHandler. Stamps every outbound collector request
    /// with the process-unique token so the /health/detail endpoint filter can
    /// distinguish "us calling ourselves" from arbitrary remote callers.
    /// </summary>
    public sealed class HealthCheckCollectorTokenHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Remove(HealthCheckCollectorAuth.HeaderName);
            request.Headers.Add(HealthCheckCollectorAuth.HeaderName, HealthCheckCollectorAuth.Token);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
