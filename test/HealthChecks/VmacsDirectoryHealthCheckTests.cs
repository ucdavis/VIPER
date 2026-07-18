using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Viper.Classes.HealthChecks;

namespace Viper.test.HealthChecks
{
    public class VmacsDirectoryHealthCheckTests
    {
        private const string BaseUrl = "https://vmacs-qa.example.edu";
        private const string Token = "TESTAUTH";

        // Minimal payloads matching Areas/Directory/Models/VMACSItem.cs.
        private const string MatchQueryXml =
            "<query><item dbfile=\"3\"><Name>Doe, John</Name></item></query>";
        private const string EmptyQueryXml = "<query><dbfile>3</dbfile></query>";

        private sealed class StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
            : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return responder(request, cancellationToken);
            }
        }

        private static IHttpClientFactory FactoryReturning(HttpMessageHandler handler)
        {
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));
            return factory;
        }

        private static IHttpClientFactory FactoryReturning(HttpStatusCode status, string body = "") =>
            FactoryReturning(new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) })));

        private static IHttpClientFactory FactoryThrowing(Exception ex) =>
            FactoryReturning(new StubHttpMessageHandler((_, _) =>
                Task.FromException<HttpResponseMessage>(ex)));

        private static HealthCheckContext CreateContext(VmacsDirectoryHealthCheck sut) => new()
        {
            Registration = new HealthCheckRegistration("campus-vmacs-directory", sut, null, null)
        };

        private static Task<HealthCheckResult> RunAsync(VmacsDirectoryHealthCheck sut) =>
            sut.CheckHealthAsync(CreateContext(sut), TestContext.Current.CancellationToken);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CheckHealthAsync_UnhealthyWhenBaseUrlNotConfigured(string? baseUrl)
        {
            var sut = new VmacsDirectoryHealthCheck(
                Substitute.For<IHttpClientFactory>(), baseUrl, Token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("Vmacs:BaseUrl is not configured", result.Description);
        }

        [Theory]
        [InlineData("not-a-url")]
        [InlineData("/relative/path")]
        [InlineData("vmacs-qa.example.edu")]
        public async Task CheckHealthAsync_UnhealthyWhenBaseUrlNotAbsolute(string baseUrl)
        {
            var sut = new VmacsDirectoryHealthCheck(
                Substitute.For<IHttpClientFactory>(), baseUrl, Token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("Vmacs:BaseUrl", result.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CheckHealthAsync_HealthyWhenTokenMissingAndSkipAllowed(string? token)
        {
            var sut = new VmacsDirectoryHealthCheck(
                Substitute.For<IHttpClientFactory>(), BaseUrl, token, healthyWhenTokenMissing: true);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("skipped", result.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task CheckHealthAsync_UnhealthyWhenTokenMissingAndSkipNotAllowed(string? token)
        {
            var sut = new VmacsDirectoryHealthCheck(
                Substitute.For<IHttpClientFactory>(), BaseUrl, token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("VmacsAuthToken is not configured", result.Description);
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task CheckHealthAsync_UnhealthyWhenEndpointRejectsAuth(HttpStatusCode status)
        {
            var sut = new VmacsDirectoryHealthCheck(FactoryReturning(status), BaseUrl, Token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("rejected the request", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_DegradedWhenEndpointReturnsServerError()
        {
            var sut = new VmacsDirectoryHealthCheck(
                FactoryReturning(HttpStatusCode.InternalServerError), BaseUrl, Token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Contains("HTTP 500", result.Description);
        }

        [Theory]
        [InlineData(MatchQueryXml)]
        [InlineData(EmptyQueryXml)]
        public async Task CheckHealthAsync_HealthyWhenEndpointReturnsParseableXml(string body)
        {
            // An empty <query> (unknown probe login) still proves endpoint + auth +
            // response shape, so it must read Healthy just like a matching record.
            var sut = new VmacsDirectoryHealthCheck(
                FactoryReturning(HttpStatusCode.OK, body), BaseUrl, Token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Contains("reachable", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_DegradedWhenEndpointReturnsUnparseableXml()
        {
            var sut = new VmacsDirectoryHealthCheck(
                FactoryReturning(HttpStatusCode.OK, "this is not xml"), BaseUrl, Token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Contains("not valid XML", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_DegradedWhenEndpointUnreachable()
        {
            var sut = new VmacsDirectoryHealthCheck(
                FactoryThrowing(new HttpRequestException("connection refused")), BaseUrl, Token);

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Contains("unreachable", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_DegradedWhenEndpointTimesOut()
        {
            var handler = new StubHttpMessageHandler(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var sut = new VmacsDirectoryHealthCheck(
                FactoryReturning(handler), BaseUrl, Token, timeout: TimeSpan.FromMilliseconds(100));

            var result = await RunAsync(sut);

            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Contains("timed out", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_DegradedWhenProbeCancelled()
        {
            // Handler honors the token so an externally-cancelled probe (app
            // shutdown / RequestAborted) surfaces as cancelled, not timed out.
            var handler = new StubHttpMessageHandler(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var sut = new VmacsDirectoryHealthCheck(FactoryReturning(handler), BaseUrl, Token);

            var result = await sut.CheckHealthAsync(
                CreateContext(sut), new CancellationToken(canceled: true));

            Assert.Equal(HealthStatus.Degraded, result.Status);
            Assert.Contains("cancelled", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_IssuesProbeQueryAgainstConfiguredBaseUrlAndToken()
        {
            var handler = new StubHttpMessageHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(EmptyQueryXml)
                }));
            var sut = new VmacsDirectoryHealthCheck(FactoryReturning(handler), BaseUrl + "/", Token);

            await RunAsync(sut);

            var uri = handler.LastRequest?.RequestUri?.ToString();
            Assert.NotNull(uri);
            // Trailing slash on the base URL must be collapsed, not doubled.
            Assert.StartsWith(BaseUrl + "/trust/query.xml", uri);
            Assert.Contains("AUTH=" + Token, uri);
        }
    }
}
