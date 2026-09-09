using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Viper.Classes.Utilities;
using Web.Authorization;
using LogLevel = NLog.LogLevel;

namespace Viper.Controllers
{
    /// <summary>
    /// OpenID Connect front-channel logout endpoint. Entra loads this URL in a hidden iframe when
    /// the user signs out anywhere in the tenant, so a sign-out from Outlook or Canvas also ends
    /// the VIPER session instead of leaving it alive until its 12 hour cookie expires.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This differs from the sign-out in <c>HomeController.Logout</c>, which is RP-initiated: the
    /// user clicks logout inside VIPER and VIPER tells Entra. Here Entra is telling VIPER, and the
    /// request arrives with no session of its own, so the <c>sid</c> in the query string is the
    /// only link back to the cookie that has to stop working.
    /// </para>
    /// <para>
    /// The relay to VIPER 1 is server to server; the browser only ever loads this URL. See
    /// <see cref="EntraIdSettings.FrontChannelLogoutForwardTo"/> for why VIPER 2 owns the URL.
    /// </para>
    /// <para>
    /// Anonymous by necessity: the caller is Entra, not a signed-in user. Two things follow. The
    /// endpoint is framed cross-site, so <c>Program.cs</c> exempts this path from the CSP that
    /// otherwise sends <c>frame-ancestors 'none'</c> and would stop the iframe loading at all. And
    /// anyone can post any <c>sid</c> to it, so it must stay a pure no-op for values that do not
    /// match a live session: revoking an unknown id costs a cache entry and nothing else.
    /// </para>
    /// </remarks>
    [AllowAnonymous]
    [Route(EntraIdSettings.FrontChannelLogoutPath)]
    public class EntraLogoutController : ControllerBase
    {
        private readonly EntraSessionRevocationStore _revocations;
        private readonly IHttpClientFactory _clientFactory;
        private readonly EntraIdSettings _settings;

        public EntraLogoutController(
            EntraSessionRevocationStore revocations,
            IHttpClientFactory clientFactory,
            IOptions<EntraIdSettings> settings)
        {
            _revocations = revocations;
            _clientFactory = clientFactory;
            _settings = settings.Value;
        }

        /// <summary>
        /// Revokes the named Entra session and notifies VIPER 1. Always answers 200 with no body:
        /// the spec asks for it, and the caller is an iframe that cannot act on an error anyway.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> FrontChannelLogout(
            [FromQuery] string? sid,
            [FromQuery] string? iss)
        {
            // Never cached: a cached 200 would swallow the next sign-out for this session.
            Response.Headers.CacheControl = "no-store";

            if (string.IsNullOrWhiteSpace(sid))
            {
                // Without a sid there is no way to tell which session ended, and signing out every
                // user of the tenant is not a reasonable reading of an unauthenticated GET.
                HttpHelper.Logger.Log(LogLevel.Warn,
                    "Front-channel logout ignored: no sid in the request.");
                return Ok();
            }

            if (!IssuerMatches(iss))
            {
                HttpHelper.Logger.Log(LogLevel.Warn,
                    "Front-channel logout ignored: unexpected iss "
                    + LogSanitizer.SanitizeString(iss)
                    + ", expected " + LogSanitizer.SanitizeString(_settings.Authority));
                return Ok();
            }

            _revocations.Revoke(sid);

            await ForwardToViperOne(sid, iss);

            return Ok();
        }

        /// <summary>
        /// True when the request names the tenant this app trusts. Entra sends the v2.0 issuer,
        /// which is exactly <see cref="EntraIdSettings.Authority"/>. A blank iss is accepted
        /// because the parameter is optional in the front-channel logout spec.
        /// </summary>
        private bool IssuerMatches(string? iss)
        {
            return string.IsNullOrWhiteSpace(iss)
                || string.Equals(iss.TrimEnd('/'), _settings.Authority.TrimEnd('/'),
                    StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Passes the notification to VIPER 1, which keeps its own server-side session rows and
        /// cannot hear from Entra directly. Failures are logged and swallowed: VIPER 2's own
        /// session is already revoked by this point, and returning an error to the iframe would
        /// not make VIPER 1 any more logged out.
        /// </summary>
        private async Task ForwardToViperOne(string sid, string? iss)
        {
            var target = _settings.FrontChannelLogoutForwardTo;

            if (string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            var client = _clientFactory.CreateClient(EntraIdSettings.FrontChannelLogoutClientName);
            var describeTarget = "Front-channel logout forward to " + LogSanitizer.SanitizeString(target);

            try
            {
                var url = QueryHelpers.AddQueryString(target, new Dictionary<string, string?>
                {
                    ["sid"] = sid,
                    ["iss"] = iss ?? string.Empty
                });

                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    HttpHelper.Logger.Log(LogLevel.Warn,
                        describeTarget + " answered " + (int)response.StatusCode + ".");
                }
            }
            // TaskCanceledException is the named client's timeout, almost always. VIPER 1 being
            // slow or down must not stall the iframe Entra is blocking on for every other app in
            // the tenant, so both failures are logged and dropped.
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                HttpHelper.Logger.Log(LogLevel.Warn, ex, describeTarget + " failed.");
            }
        }
    }
}
