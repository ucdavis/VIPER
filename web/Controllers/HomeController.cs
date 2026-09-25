using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Viper.Areas.CMS.Data;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models;
using Viper.Models.AAUD;
using Web.Authorization;
using LogLevel = NLog.LogLevel;

namespace Viper.Controllers
{
    public class HomeController : AreaController
    {
        private readonly AAUDContext _aAUDContext;
        private readonly RAPSContext _rapsContext;
        private readonly VIPERContext _viperContext;
        // An XML namespace identifier, not a network endpoint. The scheme is part of the
        // literal CAS responses are namespaced with; changing it stops the elements matching.
#pragma warning disable S5332 // Using http protocol is insecure
        private readonly XNamespace _ns = "http://www.yale.edu/tp/cas";
#pragma warning restore S5332
        private readonly IHttpClientFactory _clientFactory;
        private readonly CasSettings _settings;
        private readonly IPublicUrlService _publicUrl;
        private readonly AuthenticationSettings _authSettings;
        private readonly List<string> _casAttributesToCapture = new() { "authenticationDate", "credentialType" };
        private readonly IUserHelper _userHelper;

        public HomeController(IHttpClientFactory clientFactory, IOptions<CasSettings> settingsOptions, IPublicUrlService publicUrl, IOptions<AuthenticationSettings> authSettingsOptions, AAUDContext aAUDContext, RAPSContext rapsContext, VIPERContext viperContext)
        {
            this._clientFactory = clientFactory;
            this._settings = settingsOptions.Value;
            this._publicUrl = publicUrl;
            this._authSettings = authSettingsOptions.Value;
            this._aAUDContext = aAUDContext;
            this._rapsContext = rapsContext;
            this._viperContext = viperContext;
            this._userHelper = new UserHelper();
        }
        /// <summary>
        /// VIPER 2 home page
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [SearchName(FriendlyName = "Viper 2 Homepage")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("/[action]/")]
        [Authorize(Policy = "2faAuthentication")]
        [Permission(Allow = "SVMSecure")]
        public IActionResult Policy()
        {
            return View();
        }

#pragma warning disable S6967 // Action filter override doesn't receive model-bound data
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                         ActionExecutionDelegate next)
#pragma warning restore S6967
        {
            ViewData["ViperLeftNav"] = Nav();
            await base.OnActionExecutionAsync(context, next);
        }

        private NavMenu Nav()
        {
            var menu = new LeftNavMenu(_viperContext, _rapsContext).GetLeftNavMenus(friendlyName: "viper-home")?.FirstOrDefault();
            if (menu != null)
            {
                ConvertNavLinksForDevelopment(menu);
            }
            return menu ?? new NavMenu("", new List<NavMenuItem>());
        }

        /// <summary>
        /// Login function -- sends the user to the enabled sign-in provider, no VIEW
        /// </summary>
        /// <remarks>
        /// Provider-aware so every existing "Log in" link keeps working across the CAS to Entra ID
        /// cutover, which is a config switch rather than a code change.
        /// </remarks>
        [Route("/[action]")]
        [AllowAnonymous]
        [SearchExclude]
        public IActionResult Login([FromQuery] string? ReturnUrl = null)
        {
            // Resolved before dispatching so the /api 401 contract holds for either provider.
            if (!TryResolveLoginReturnUrl(ReturnUrl, out var returnUrl))
            {
                return Unauthorized();
            }

            if (_authSettings.EntraIdEnabled)
            {
                // No picker from here. Entra signs a single signed-in account straight through and
                // raises its own picker only when several match, which is what we want.
                return RedirectToAction(nameof(EntraLogin), new { ReturnUrl });
            }

            var authorizationEndpoint = _settings.CasBaseUrl + "login?service=" + WebUtility.UrlEncode(BuildRedirectUri(new PathString("/CasLogin")) + "?ReturnUrl=" + WebUtility.UrlEncode(returnUrl));

            return new RedirectResult(authorizationEndpoint);
        }

        /// <summary>
        /// Entra ID login -- challenges the OpenID Connect handler, no VIEW
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        [SearchExclude]
#pragma warning disable S6967 // Action only reads ReturnUrl and selectAccount, no model binding required
        public IActionResult EntraLogin(
            [FromQuery] string? ReturnUrl = null,
            [FromQuery] bool selectAccount = false)
#pragma warning restore S6967
        {
            if (!_authSettings.EntraIdEnabled)
            {
                return NotFound();
            }

            if (!TryResolveLoginReturnUrl(ReturnUrl, out var returnUrl))
            {
                return Unauthorized();
            }

            // No /CasLogin counterpart is needed: the OIDC handler owns its callback path, carries
            // RedirectUri through the OAuth state, and redirects there itself once the shared
            // cookie is issued.
            var properties = new AuthenticationProperties
            {
                RedirectUri = string.IsNullOrEmpty(returnUrl) ? Url.Content("~/") : returnUrl
            };

            if (selectAccount)
            {
                // Set only by the "use a different account" link. Without it a user whose other
                // account is the last one Entra still holds gets signed back into it silently,
                // with nothing offering a choice. Forging the flag costs an attacker an account
                // picker, so it needs no protection. The handler reads this parameter itself, so
                // no redirect event is involved; suppressing domain_hint for it does need one.
                properties.SetParameter(OpenIdConnectParameterNames.Prompt, "select_account");
            }

            return Challenge(properties, EntraIdClaimMapper.AuthenticationScheme);
        }

        // Resolves where to send the user after a successful sign-in, shared by every provider so
        // the ReturnUrl rules cannot drift between them. Returns false when the target is an /api
        // path, which must get a 401 rather than be bounced through an interactive login.
        private bool TryResolveLoginReturnUrl(string? requestedReturnUrl, out string returnUrl)
        {
            // Resolve app-relative "~/..." before validating, so the /api guard below cannot be
            // bypassed and we never forward an invalid browser URL to a provider.
            requestedReturnUrl = NormalizeAppRelativeUrl(requestedReturnUrl);

            // Off-site targets fall back to the app root: the OIDC handler would follow one as an
            // open redirect, and CasLogin's LocalRedirect would throw on it after a good sign-in.
            if (!Url.IsLocalUrl(requestedReturnUrl))
            {
                requestedReturnUrl = null;
            }

            // Default to the application root under the deployed PathBase ("" locally, "/2" on TEST/PROD).
            var appRoot = Request.PathBase.Value ?? string.Empty;
            returnUrl = string.IsNullOrEmpty(requestedReturnUrl) ? appRoot : requestedReturnUrl;

            // Strip the PathBase (e.g. "/2") before the /api guard so a base-prefixed
            // "/2/api/..." ReturnUrl can't slip past this root-relative check.
            var apiCheckUrl = StripPathBase(returnUrl, Request.PathBase.Value);
            if (apiCheckUrl != null && IsApiPath(apiCheckUrl))
            {
                return false;
            }

            // A local URL outside the PathBase would land in VIPER 1 on TEST/PROD.
            if (!IsUnderPathBase(returnUrl))
            {
                returnUrl = appRoot;
            }

            return true;
        }

        // Url.IsLocalUrl accepts app-relative "~/..." URLs, but browsers and CAS don't
        // understand the "~", so resolve "~/..." to "{PathBase}/..." before validating or
        // redirecting. Leaves all other values (including null) unchanged.
        private string? NormalizeAppRelativeUrl(string? returnUrl)
            => returnUrl != null && returnUrl.StartsWith("~/") ? Request.PathBase.Value + returnUrl[1..] : returnUrl;

        // Routing is case-insensitive, so the /api guard must be too; matching on a segment
        // boundary keeps non-API paths that merely start with "api" (e.g. "/apiary") out of
        // the guard. internal (not private) so it is unit-testable via InternalsVisibleTo.
        internal static bool IsApiPath(string url)
        {
            if (!url.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return url.Length == 4 || url[4] is '/' or '?' or '#';
        }

        // True when a local URL stays inside this app: always locally (no PathBase), and on TEST/PROD
        // only under "/2". Uses StripPathBase's segment-boundary match, so "/22/..." is outside.
        private bool IsUnderPathBase(string? url)
        {
            var pathBase = Request.PathBase.Value;
            return string.IsNullOrEmpty(pathBase) || StripPathBase(url, pathBase) != url;
        }

        // Removes the application's PathBase prefix (e.g. "/2" in a subpath deployment) from a return
        // URL so the splash classifier and label resolver can treat it as root-relative. Matches on a
        // segment boundary so "/2" never strips from an unrelated "/22/...". Returns the URL unchanged
        // when there is no base to strip (e.g. local dev, where PathBase is empty).
        // internal (not private) so it is unit-testable via InternalsVisibleTo.
        internal static string? StripPathBase(string? url, string? pathBase)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(pathBase))
            {
                return url;
            }

            if (url.StartsWith(pathBase, StringComparison.OrdinalIgnoreCase)
                && (url.Length == pathBase.Length || url[pathBase.Length] is '/' or '?' or '#'))
            {
                return url[pathBase.Length..];
            }

            return url;
        }

        [Route("/[action]")]
        [SearchExclude]
        public IActionResult RefreshSession()
        {
            SessionTimeoutService.UpdateSessionTimeout(_viperContext);
            return Ok(SessionTimeoutService.GetSessionTimeout(_viperContext));
        }

        /// <summary>
        /// CAS Login function -- redirects to original page, no VIEW
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        [SearchExclude]
        public async Task<IActionResult> CasLogin([FromQuery] string? ticket = null, [FromQuery] string? ReturnUrl = null)
        {
            if (!_authSettings.CasEnabled)
            {
                return NotFound();
            }

            return await AuthenticateCasLogin(ticket, ReturnUrl);
        }

        //TODO - consider implementing IP restrictions on this method to only allow emulation from in school or on VPN locations
        /// <summary>
        /// Emulate a user
        /// </summary>
        /// <param name="loginId">The login id of the user we are emulating</param>
        [Route("/[action]/{loginId}")]
        [Authorize(Policy = "2faAuthentication")]
        [Permission(Allow = "SVMSecure.SU")]
        public IActionResult EmulateUser(string loginId)
        {
            AaudUser? emulatedUser = _userHelper.GetByLoginId(_aAUDContext, loginId);

            string? trueLoginId = _userHelper.GetCurrentUser()?.LoginId;

            if (emulatedUser != null && trueLoginId != null)
            {
                var protector = HttpHelper.DataProtectionProvider?.CreateProtector("Viper.Emulation", trueLoginId);

                if (protector != null && emulatedUser.LoginId != null)
                {
                    string encryptedEmulatedLoginId = protector.Protect(emulatedUser.LoginId);

                    // set emulating cached item to expire after 30 minutes of inactivity
                    HttpHelper.Cache?.Set(ClaimsTransformer.EmulationCacheNamePrefix + trueLoginId, encryptedEmulatedLoginId, (new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30))));
                    return new RedirectResult("~/");
                }

            }

            return new RedirectResult("~/Error");

        }

        /// <summary>
        /// Clears the emulation cache for the user
        /// </summary>
        [Route("/[action]")]
        public IActionResult ClearEmulation()
        {
            AaudUser? user = _userHelper.GetTrueCurrentUser();
            string? trueLoginId = user?.LoginId;

            if (trueLoginId != null && HttpHelper.Cache != null)
            {
                HttpHelper.Cache.Remove(ClaimsTransformer.EmulationCacheNamePrefix + trueLoginId);
            }

            return new RedirectResult("~/");
        }

        /// <summary>
        /// Clears the cache
        /// </summary>
        [Route("/[action]")]
        [Authorize(Roles = "VMDO SVM-IT", Policy = "2faAuthentication")]
#pragma warning disable S3011 // Reflection used to access internal MemoryCache entries - intentional for cache clearing
        public IActionResult ClearCache()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var entries = HttpHelper.Cache?.GetType().GetField("_entries", flags)?.GetValue(HttpHelper.Cache);
#pragma warning restore S3011

            if (entries is IDictionary cacheEntries)
            {
                foreach (string key in cacheEntries.Keys)
                {
                    HttpHelper.Cache?.Remove(key);
                }

            }

            return new RedirectResult("~/");
        }

        /// <summary>
        /// Error page. When no statusCode is provided, shows general error.
        /// When statusCode is provided (e.g., /Error/404), shows appropriate status page.
        /// </summary>
        /// <param name="statusCode">HTTP status code (optional)</param>
        [Route("/[action]")]
        [Route("/[action]/{statusCode:int}")]
        [AllowAnonymous]
        // Anti-forgery is irrelevant here: the error page is anonymous, binds one int? route
        // value, and mutates no state. Requiring a token would break the 404/500 handler,
        // which is re-executed on requests that never carried one.
#pragma warning disable S4502 // Disabling CSRF protections is security-sensitive
        [IgnoreAntiforgeryToken]
#pragma warning restore S4502
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [SearchExclude]
#pragma warning disable S6967 // Error handler uses simple route parameter, not form data requiring validation
        public IActionResult Error(int? statusCode = null)
#pragma warning restore S6967
        {
            ViewBag.errorMessage = HttpContext.Items["ErrorMessage"];

            if (statusCode.HasValue)
            {
                string? viewName;
                switch (statusCode)
                {
                    case 403:
                        Response.StatusCode = 403;
                        viewName = statusCode.ToString();
                        break;
                    default:
                        viewName = "StatusCode";
                        break;
                }

                return View(viewName, (HttpStatusCode)statusCode.Value);
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Where a failed Entra sign-in lands. Offers the account picker, since a plain retry would
        /// silently reuse whichever account Entra still holds.
        /// </summary>
        /// <param name="reason"><see cref="EntraIdClaimMapper.NoAccountReason"/> when the account has no AAUD user.</param>
        [Route("/[action]")]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [SearchExclude]
#pragma warning disable S6967 // Reads one optional query value, no model binding required
        public IActionResult SignInProblem([FromQuery] string? reason = null)
#pragma warning restore S6967
        {
            ViewData["NoAccount"] = string.Equals(reason, EntraIdClaimMapper.NoAccountReason, StringComparison.Ordinal);
            return View();
        }

        /// <summary>
        /// Logout function -- clears the local session then signs out of the provider, no VIEW
        /// </summary>
        /// <remarks>
        /// POST only, so a third-party page cannot sign a user out with an &lt;img&gt; tag. Callers
        /// post a form rather than fetch, because the response is a redirect the browser has to
        /// follow to reach the provider's sign-out.
        /// </remarks>
        [HttpPost]
        [Route("/[action]")]
        [SearchExclude]
        public async Task<IActionResult> Logout()
        {
            _userHelper.ClearCachedRolesAndPermissions(_userHelper.GetCurrentUser());

            // Read the provider off the principal before signing out, while the claims still exist.
            var signedInWithEntraId = string.Equals(
                User.FindFirst(ClaimTypes.AuthenticationMethod)?.Value,
                EntraIdClaimMapper.AuthenticationMethod,
                StringComparison.Ordinal);
            var logoutHint = User.FindFirst(EntraIdClaimMapper.LoginHintClaimType)?.Value;

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (signedInWithEntraId)
            {
                if (_authSettings.EntraIdEnabled)
                {
                    // Federated sign-out. Without it the Entra session outlives the VIPER cookie
                    // and the next sign-in silently reuses it, which looks like logout did nothing.
                    var properties = new AuthenticationProperties { RedirectUri = Url.Content("~/") };

                    if (!string.IsNullOrWhiteSpace(logoutHint))
                    {
                        // Names the account being ended, so a user with two signed in is not asked
                        // which. Sessions predating the claim just omit it and behave as before.
                        properties.Items[EntraIdClaimMapper.LogoutHintPropertyKey] = logoutHint;
                    }

                    return SignOut(properties, EntraIdClaimMapper.AuthenticationScheme);
                }

                // Entra was switched off while this cookie was still valid, so its handler is no
                // longer registered and the end_session endpoint is unreachable; the upstream Entra
                // session has to age out on its own. Falling through to CAS logout would be wrong:
                // this user never had a CAS session to end.
                return LocalRedirect("~/");
            }

            if (!_authSettings.CasEnabled)
            {
                // CAS has been switched off, so there is no CAS session left to end. This also
                // covers a stale CAS cookie still in flight after the cutover.
                return LocalRedirect("~/");
            }

            // Send homepage link after CAS logout
            var returnUrl = WebUtility.UrlEncode(_publicUrl.BaseUrl);
            return new RedirectResult(_settings.CasBaseUrl + "logout?service=" + returnUrl);
        }

        [Route("/[action]")]
        [SearchExclude]
        public IActionResult MyPermissions()
        {
            var u = _userHelper.GetCurrentUser();
            if (u != null)
            {
                ViewData["Permissions"] = _userHelper.GetAllPermissions(_rapsContext, u)
                    .OrderBy(p => p.Permission)
                    .ToList();

                ViewData["Roles"] = _userHelper.GetRoles(_rapsContext, u)
                    .OrderBy(r => r.Role)
                    .ToList();

                ViewData["Has2FA"] = DuoAuthenticationRequirement.HasDuoAuthentication(HttpContext.User);
            }
            return View();
        }



        /// <summary>
        /// Utility function for creating redirect URLs. Built from the configured canonical
        /// origin, never the request Host, so a forged Host cannot poison a CAS callback.
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns>Compiled URL</returns>
        private string BuildRedirectUri(string targetPath)
        {
            return _publicUrl.BuildUrl(targetPath);
        }

        /// <summary>
        /// Processes the CAS login and sets the user
        /// </summary>
        private async Task<IActionResult> AuthenticateCasLogin(string? ticket, string? returnUrl)
        {
            string service = WebUtility.UrlEncode(BuildRedirectUri(Request.Path) + "?ReturnUrl=" + WebUtility.UrlEncode(returnUrl));

            var client = _clientFactory.CreateClient("CAS");

            try
            {
                var response = await client.GetAsync(_settings.CasBaseUrl + "p3/serviceValidate?ticket=" + ticket + "&service=" + service, HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var doc = XDocument.Parse(responseBody);

                var serviceResponse = doc.Element(_ns + "serviceResponse");
                var successNode = serviceResponse?.Element(_ns + "authenticationSuccess");
                var userNode = successNode?.Element(_ns + "user");
                var validatedUserName = userNode?.Value;

                // Log the sanitized CAS response when no username comes back, to help diagnose unexpected 403 (Access Denied) errors
                if (string.IsNullOrEmpty(validatedUserName))
                {
                    HttpHelper.Logger.Log(LogLevel.Warn, "No username. CAS response: " + LogSanitizer.SanitizeString(doc.ToString()));
                }

                if (!string.IsNullOrEmpty(validatedUserName))
                {
                    var claimsIdentity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, validatedUserName), new Claim(ClaimTypes.NameIdentifier, validatedUserName), new Claim(ClaimTypes.AuthenticationMethod, "CAS") }, CookieAuthenticationDefaults.AuthenticationScheme);

                    // successNode is guaranteed non-null here: validatedUserName is derived from successNode?.Element(user)?.Value.
                    XElement? attributesNode = successNode!.Element(_ns + "attributes");
                    if (attributesNode != null)
                    {
                        foreach (string attributeName in _casAttributesToCapture)
                        {
                            foreach (var element in attributesNode.Elements(_ns + attributeName))
                            {
                                claimsIdentity.AddClaim(new Claim(element.Name.LocalName, element.Value));
                            }
                        }
                    }

                    var user = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);

                    return new LocalRedirectResult(!String.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "/");
                }
            }
            catch (TaskCanceledException ex)
            {
                // usually caused because the user aborts the page load (HttpContext.RequestAborted)
                HttpHelper.Logger.Log(LogLevel.Info, ex, "TaskCanceledException during CAS login");
            }

            return new ForbidResult();
        }
    }
}
