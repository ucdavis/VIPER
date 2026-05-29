using System.Net;
using Viper.Classes.HealthChecks;

namespace Viper.test.HealthChecks
{
    public class HealthCheckCollectorTokenHandlerTests
    {
        [Fact]
        public async Task SendAsync_StampsTokenHeader()
        {
            var (handler, recorder) = BuildHandler();
            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/health/detail");

            using var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(recorder.LastRequest);
            var values = recorder.LastRequest!.Headers.GetValues(HealthCheckCollectorAuth.HeaderName).ToList();
            Assert.Single(values);
            Assert.Equal(HealthCheckCollectorAuth.Token, values[0]);
        }

        [Fact]
        public async Task SendAsync_ReplacesExistingTokenHeader()
        {
            var (handler, recorder) = BuildHandler();
            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/health/detail");
            // Simulate a stale value left on the request before our handler runs.
            request.Headers.Add(HealthCheckCollectorAuth.HeaderName, "stale-value");

            await invoker.SendAsync(request, CancellationToken.None);

            var values = recorder.LastRequest!.Headers.GetValues(HealthCheckCollectorAuth.HeaderName).ToList();
            Assert.Single(values);
            Assert.Equal(HealthCheckCollectorAuth.Token, values[0]);
        }

        [Fact]
        public async Task SendAsync_ForwardsResponseFromInnerHandler()
        {
            var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
            var handler = new HealthCheckCollectorTokenHandler { InnerHandler = inner };
            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/health/detail");

            using var response = await invoker.SendAsync(request, CancellationToken.None);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }

        private static (HealthCheckCollectorTokenHandler handler, RecordingHandler recorder) BuildHandler()
        {
            var inner = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var handler = new HealthCheckCollectorTokenHandler { InnerHandler = inner };
            return (handler, inner);
        }

        /// <summary>
        /// Inner DelegatingHandler that records the request it received and returns
        /// a configurable response, so tests can assert what flowed downstream.
        /// </summary>
        private sealed class RecordingHandler : DelegatingHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

            public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
            {
                _respond = respond;
            }

            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(_respond(request));
            }
        }
    }
}
