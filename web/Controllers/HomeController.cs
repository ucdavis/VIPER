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
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
        private readonly List<string> _casAttributesToCapture = new() { "authenticationDate", "credentialType" };
        private readonly IUserHelper _userHelper;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;

        public HomeController(IHttpClientFactory clientFactory, IOptions<CasSettings> settingsOptions, IPublicUrlService publicUrl, AAUDContext aAUDContext, RAPSContext rapsContext, VIPERContext viperContext, IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            this._clientFactory = clientFactory;
            this._settings = settingsOptions.Value;
            this._publicUrl = publicUrl;
            this._aAUDContext = aAUDContext;
            this._rapsContext = rapsContext;
            this._viperContext = viperContext;
            this._userHelper = new UserHelper();
            this._actionDescriptorProvider = actionDescriptorProvider;
        }
        /// <summary>
        /// VIPER 2 home page
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [SearchName(FriendlyName = "Viper 2 Homepage")]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                // Anonymous splash served in-place at "/". The authenticated home response
                // below keeps its default caching.
                return WelcomeSplash(returnUrl: null, destinationLabel: null);
            }
            return View();
        }

        /// <summary>
        /// Unauthenticated landing/splash page. Anonymous users see the welcome splash;
        /// authenticated users are redirected to the validated ReturnUrl or "/".
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        [SearchExclude]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
#pragma warning disable S6967 // Action only reads ReturnUrl, no model binding required
        public IActionResult Welcome([FromQuery] string? ReturnUrl = null)
#pragma warning restore S6967
        {
            // Normalize "~/..." to "/..." (mirrors Login) so the loop-guard catches
            // ~/welcome and ~/login and we never emit a "~/" redirect target.
            ReturnUrl = NormalizeAppRelativeUrl(ReturnUrl);

            // In a subpath deployment the ReturnUrl carries the PathBase (e.g. "/2/ClinicalScheduler"),
            // so strip it once here — the classifier and label resolver both need it root-relative.
            // The full ReturnUrl is preserved for the redirect/links back.
            var relativeReturnUrl = StripPathBase(ReturnUrl, Request.PathBase.Value);

            if (!IsSafeReturnUrl(ReturnUrl))
            {
                ReturnUrl = null;
                relativeReturnUrl = null;
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                // "~/" (not "/") so the app root keeps the PathBase ("/2/") in a subpath deployment
                // instead of redirecting out to the domain root (the legacy site).
                return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? "~/" : ReturnUrl);
            }

            // Only passive arrivals get the splash: the bare site root or a top-level area
            // landing page (e.g. "/ClinicalScheduler"). A deep link (e.g. "/ClinicalScheduler/rotation")
            // skips the interstitial and goes straight to CAS so we don't interrupt a targeted workflow.
            if (!IsSplashTarget(relativeReturnUrl, GetAreaNames(_actionDescriptorProvider)))
            {
                return RedirectToAction(nameof(Login), new { ReturnUrl });
            }

            return WelcomeSplash(ReturnUrl, WelcomePageHelper.ResolveDestinationLabel(relativeReturnUrl));
        }

        // Single owner of the splash's ViewData and cache-header contract, shared by /welcome and by
        // the anonymous "/" landing. Welcome carries [ResponseCache(NoStore, Location=None)]; Index
        // has no such attribute, so the headers are set here to keep the two responses identical.
        private IActionResult WelcomeSplash(string? returnUrl, string? destinationLabel)
        {
            Response.Headers["Cache-Control"] = "no-store,no-cache";
            Response.Headers["Pragma"] = "no-cache";

            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Hero"] = PickRandomHeroKey();
            ViewData["DestinationLabel"] = destinationLabel;

            return View("Welcome");
        }

        private static readonly string[] _heroKeys =
        {
            "svm_building",
            "vetmed_admin",
            "ophthalmology",
            "guinea_pig",
            "horse_foal",
        };

        private static string PickRandomHeroKey()
        {
            return _heroKeys[Random.Shared.Next(_heroKeys.Length)];
        }

        // Url.IsLocalUrl accepts app-relative "~/..." URLs, but browsers and CAS don't
        // understand the "~", so normalize "~/..." to "/..." before validating or
        // redirecting. Leaves all other values (including null) unchanged.
        private static string? NormalizeAppRelativeUrl(string? returnUrl)
            => returnUrl != null && returnUrl.StartsWith("~/") ? returnUrl[1..] : returnUrl;

        // The ReturnUrl contract shared by every auth entry point (/welcome, /login, /CasLogin), so the
        // three cannot drift apart: the URL must be local, must not point back at an auth entry point,
        // and must not carry a dot-segment. Normalizes and strips the PathBase internally, since the
        // path guards below all compare root-relative paths.
        // internal (not private) so the shared guard is unit-testable via InternalsVisibleTo.
        internal bool IsSafeReturnUrl(string? returnUrl)
        {
            if (!Url.IsLocalUrl(returnUrl))
            {
                return false;
            }

            var path = StripPathBase(NormalizeAppRelativeUrl(returnUrl), Request.PathBase.Value);
            return !IsAuthEntryPath(path) && !ContainsDotSegment(path);
        }

        // Everything before the query or fragment: the ReturnUrl guards all classify on the path alone.
        private static string PathWithoutQuery(string url)
        {
            int cut = url.IndexOfAny(['?', '#']);
            return cut >= 0 ? url[..cut] : url;
        }

        // The auth entry points, which must never be a ReturnUrl: /welcome and /login would
        // redirect-loop, and /caslogin would re-enter the ticket handler without a ticket and
        // 403 a user who just signed in successfully.
        private static readonly string[] _authEntryPaths = ["/welcome", "/login", "/caslogin"];

        // internal (not private) so the redirect-loop guard is unit-testable via InternalsVisibleTo.
        internal static bool IsAuthEntryPath(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            var path = PathWithoutQuery(url).TrimEnd('/');

            return _authEntryPaths.Contains(path, StringComparer.OrdinalIgnoreCase);
        }

        // Browsers resolve dot-segments before issuing the request, and the URL spec counts the
        // percent-encoded spellings too: "%2e" is ".", and ".%2e"/"%2e."/"%2e%2e" are "..", all
        // ASCII case-insensitive.
        private static readonly string[] _dotSegments = [".", "..", "%2e", "%2e%2e", ".%2e", "%2e."];

        // The Vue guard rejects "../" and any "%2e" outright (RequireLogin.ts). Match it here: a
        // ReturnUrl like "/Effort/../api/x" passes IsLocalUrl and the root-relative /api check, but the
        // browser resolves it to "/api/x" after the CAS round trip and dumps the user on a JSON 401.
        // internal (not private) so it is unit-testable via InternalsVisibleTo.
        internal static bool ContainsDotSegment(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            return PathWithoutQuery(url)
                .Split('/')
                .Any(segment => _dotSegments.Contains(segment, StringComparer.OrdinalIgnoreCase));
        }

        // Controllers under web/Areas live in the "Viper.Areas.<Area>.…" namespace. Deriving the area
        // set from controller namespaces (rather than the [Area] route value) covers every area —
        // including SPA areas whose controllers are API-only and carry no [Area] attribute — and needs
        // no hand-maintained list: add an area the usual way and it is picked up automatically.
        private const string AreaNamespacePrefix = "Viper.Areas.";

        // Reference type (not a tuple) so the field assignment below is atomic: a multi-word struct
        // could be read torn by a concurrent request mid-write.
        private sealed record AreaNameCache(ActionDescriptorCollection Source, HashSet<string> Areas);

        // Cached per descriptor collection: the collection is immutable and replaced wholesale
        // (new instance) only when endpoints change, so the area set is derived once instead of
        // per anonymous /welcome request. Benign race: concurrent first requests may each compute
        // the set; last writer wins with an identical result.
        private static AreaNameCache? _areaNamesCache;

        // The set of top-level area names (e.g. "Effort", "ClinicalScheduler"). Used to tell an area
        // landing page ("/Effort" → splash) apart from a deep link ("/Effort/Reports" → CAS).
        private static HashSet<string> GetAreaNames(IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            var descriptors = actionDescriptorProvider.ActionDescriptors;
            var cache = _areaNamesCache;
            if (cache == null || !ReferenceEquals(cache.Source, descriptors))
            {
                var areas = descriptors.Items
                    .OfType<ControllerActionDescriptor>()
                    .Select(d => AreaFromControllerNamespace(d.ControllerTypeInfo.Namespace))
                    .Where(area => area != null)
                    .Select(area => area!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                cache = new AreaNameCache(descriptors, areas);
                _areaNamesCache = cache;
            }

            return cache.Areas;
        }

        // Extracts the area segment from a controller namespace, e.g. "Viper.Areas.Effort.Controllers"
        // → "Effort". Returns null for non-area namespaces. internal so it is unit-testable.
        internal static string? AreaFromControllerNamespace(string? ns)
        {
            if (ns == null || !ns.StartsWith(AreaNamespacePrefix, StringComparison.Ordinal))
            {
                return null;
            }

            var rest = ns[AreaNamespacePrefix.Length..];
            int dot = rest.IndexOf('.');
            var area = dot >= 0 ? rest[..dot] : rest;
            return area.Length == 0 ? null : area;
        }

        // The welcome splash is reserved for passive arrivals: the bare site root or a top-level
        // area landing page (a single path segment matching a registered area). Anything deeper is
        // a deep link that should bypass the interstitial. Null/empty ReturnUrl is the front door.
        // internal (not private) so the classifier is unit-testable via InternalsVisibleTo.
        internal static bool IsSplashTarget(string? url, ISet<string> areaNames)
        {
            if (string.IsNullOrEmpty(url))
            {
                return true;
            }

            var path = PathWithoutQuery(url).Trim('/');

            if (path.Length == 0)
            {
                return true;
            }

            if (path.Contains('/'))
            {
                return false;
            }

            return areaNames.Contains(path);
        }

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
        /// Login function -- redirects to CAS, no VIEW
        /// </summary>
        [Route("/[action]")]
        [AllowAnonymous]
        [SearchExclude]
        public IActionResult Login([FromQuery] string? ReturnUrl = null)
        {
            // Normalize app-relative "~/..." to "/..." before validating, so the
            // /api guard below cannot be bypassed and we never forward an invalid
            // browser URL to CAS.
            ReturnUrl = NormalizeAppRelativeUrl(ReturnUrl);

            if (!IsSafeReturnUrl(ReturnUrl))
            {
                ReturnUrl = null;
            }

            // The application root under the deployed PathBase ("" locally, "/2" on TEST/PROD).
            // Read from the request rather than derived from GetRootURL(), which now returns the
            // configured canonical origin and so no longer cancels against the request authority.
            string returnURL = Request.PathBase.Value ?? string.Empty;

            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                returnURL = ReturnUrl;
            }

            // Strip the PathBase (e.g. "/2") before the /api guard so a base-prefixed
            // "/2/api/..." ReturnUrl can't slip past this root-relative check and get
            // forwarded to CAS.
            var apiCheckUrl = StripPathBase(returnURL, Request.PathBase.Value);
            if (apiCheckUrl != null && IsApiPath(apiCheckUrl))
            {
                return Unauthorized();
            }

            var authorizationEndpoint = _settings.CasBaseUrl + "login?service=" + WebUtility.UrlEncode(BuildRedirectUri(new PathString("/CasLogin")) + "?ReturnUrl=" + WebUtility.UrlEncode(returnURL));

            return new RedirectResult(authorizationEndpoint);
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
        /// Logout function -- redirects to CAS logout, no VIEW
        /// </summary>
        /// <returns></returns>
        [Route("/[action]")]
        [SearchExclude]
        public async Task<IActionResult> Logout()
        {
            _userHelper.ClearCachedRolesAndPermissions(_userHelper.GetCurrentUser());
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

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

                    // Same contract as /welcome and /login. This is the redirect the browser actually
                    // follows after CAS, so a ReturnUrl that arrived via a hand-crafted service URL
                    // (bypassing those two) is dropped here too.
                    if (!IsSafeReturnUrl(returnUrl))
                    {
                        returnUrl = null;
                    }

                    // "~/" (not "/") so a subpath deployment ("/2") lands on the app root, not the domain root.
                    return new LocalRedirectResult(!String.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "~/");
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
