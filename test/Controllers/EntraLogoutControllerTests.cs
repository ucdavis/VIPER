using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Viper.Controllers;
using Web.Authorization;

namespace Test.Controllers
{
    // Entra calls this endpoint from a hidden iframe and ignores whatever it says, so the only
    // behavior worth testing is the side effects: which sessions get revoked, and what VIPER 1 is
    // told. Getting the guards wrong in the permissive direction turns an anonymous GET into a way
    // to sign other people out.
    public class EntraLogoutControllerTests
    {
        private const string Tenant = "tenant-id";
        private const string ExpectedIssuer = "https://login.microsoftonline.com/tenant-id/v2.0";
        private const string ViperOne = "https://viper1.example/public/entra/frontchannel-logout.cfm";

        private sealed class RecordingHandler : HttpMessageHandler
        {
            public List<Uri> Requests { get; } = [];
            public Exception? ThrowOnSend { get; set; }
            public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request.RequestUri!);

                return ThrowOnSend != null
                    ? Task.FromException<HttpResponseMessage>(ThrowOnSend)
                    : Task.FromResult(new HttpResponseMessage(StatusCode));
            }
        }

        private static (EntraLogoutController Controller, EntraSessionRevocationStore Store, RecordingHandler Handler)
            Build(string? forwardTo = ViperOne)
        {
            var store = new EntraSessionRevocationStore(
                new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromHours(12));

            var handler = new RecordingHandler();
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>())
                .Returns(_ => new HttpClient(handler, disposeHandler: false));

            var settings = new EntraIdSettings
            {
                TenantId = Tenant,
                ClientId = "client-id",
                FrontChannelLogoutForwardTo = forwardTo
            };

            var controller = new EntraLogoutController(store, factory, Options.Create(settings))
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            return (controller, store, handler);
        }

        [Fact]
        public async Task FrontChannelLogout_ValidRequest_RevokesTheSession()
        {
            var (controller, store, _) = Build();

            await controller.FrontChannelLogout("session-a", ExpectedIssuer);

            Assert.True(store.IsRevoked("session-a"));
        }

        [Fact]
        public async Task FrontChannelLogout_ValidRequest_ForwardsSidAndIssToViperOne()
        {
            var (controller, _, handler) = Build();

            await controller.FrontChannelLogout("session-a", ExpectedIssuer);

            var forwarded = Assert.Single(handler.Requests);

            Assert.Equal("https://viper1.example/public/entra/frontchannel-logout.cfm",
                forwarded.GetLeftPart(UriPartial.Path));
            Assert.Contains("sid=session-a", forwarded.Query, StringComparison.Ordinal);
            Assert.Contains(ExpectedIssuer, Uri.UnescapeDataString(forwarded.Query),
                StringComparison.Ordinal);
        }

        // No sid means Entra did not say which session ended. Revoking nothing is the only safe
        // reading; the alternative is signing out every user in the tenant on an anonymous GET.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task FrontChannelLogout_NoSid_RevokesNothingAndDoesNotForward(string? sid)
        {
            var (controller, _, handler) = Build();

            var result = await controller.FrontChannelLogout(sid, ExpectedIssuer);

            Assert.Empty(handler.Requests);
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task FrontChannelLogout_IssuerFromAnotherTenant_IsIgnored()
        {
            var (controller, store, handler) = Build();

            await controller.FrontChannelLogout(
                "session-a", "https://login.microsoftonline.com/someone-else/v2.0");

            Assert.False(store.IsRevoked("session-a"));
            Assert.Empty(handler.Requests);
        }

        // iss is optional in the front-channel logout spec, so its absence must not block a
        // sign-out that is otherwise well formed.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task FrontChannelLogout_NoIssuer_StillRevokes(string? iss)
        {
            var (controller, store, _) = Build();

            await controller.FrontChannelLogout("session-a", iss);

            Assert.True(store.IsRevoked("session-a"));
        }

        [Fact]
        public async Task FrontChannelLogout_IssuerDiffersOnlyByTrailingSlash_StillRevokes()
        {
            var (controller, store, _) = Build();

            await controller.FrontChannelLogout("session-a", ExpectedIssuer + "/");

            Assert.True(store.IsRevoked("session-a"));
        }

        // VIPER 1 being down or slow must not cost VIPER 2 its own sign-out, and must not hand
        // Entra an error for an iframe that cannot act on one.
        [Fact]
        public async Task FrontChannelLogout_ForwardThrows_StillRevokesAndAnswersOk()
        {
            var (controller, store, handler) = Build();
            handler.ThrowOnSend = new HttpRequestException("VIPER 1 is unreachable");

            var result = await controller.FrontChannelLogout("session-a", ExpectedIssuer);

            Assert.True(store.IsRevoked("session-a"));
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task FrontChannelLogout_ForwardTimesOut_StillRevokesAndAnswersOk()
        {
            var (controller, store, handler) = Build();
            handler.ThrowOnSend = new TaskCanceledException("timed out");

            var result = await controller.FrontChannelLogout("session-a", ExpectedIssuer);

            Assert.True(store.IsRevoked("session-a"));
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task FrontChannelLogout_ForwardAnswersError_StillRevokesAndAnswersOk()
        {
            var (controller, store, handler) = Build();
            handler.StatusCode = HttpStatusCode.NotFound;

            var result = await controller.FrontChannelLogout("session-a", ExpectedIssuer);

            Assert.True(store.IsRevoked("session-a"));
            Assert.IsType<OkResult>(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task FrontChannelLogout_NoForwardTargetConfigured_StillRevokes(string? forwardTo)
        {
            var (controller, store, handler) = Build(forwardTo);

            await controller.FrontChannelLogout("session-a", ExpectedIssuer);

            Assert.True(store.IsRevoked("session-a"));
            Assert.Empty(handler.Requests);
        }

        // A cached 200 would swallow the next sign-out for this session.
        [Fact]
        public async Task FrontChannelLogout_SetsNoStore()
        {
            var (controller, _, _) = Build();

            await controller.FrontChannelLogout("session-a", ExpectedIssuer);

            Assert.Equal("no-store", controller.Response.Headers.CacheControl);
        }
    }
}
