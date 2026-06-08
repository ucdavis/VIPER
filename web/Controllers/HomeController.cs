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
using Microsoft.AspNetCore.Http.Extensions;
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
        private readonly XNamespace _ns = "http://www.yale.edu/tp/cas";
        private readonly IHttpClientFactory _clientFactory;
        private readonly CasSettings _settings;
        private readonly List<string> _casAttributesToCapture = new() { "authenticationDate", "credentialType" };
        private readonly IUserHelper _userHelper;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;

        public HomeController(IHttpClientFactory clientFactory, IOptions<CasSettings> settingsOptions, AAUDContext aAUDContext, RAPSContext rapsContext, VIPERContext viperContext, IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            this._clientFactory = clientFactory;
            this._settings = settingsOptions.Value;
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
                // Anonymous splash served in-place at "/": mirror /welcome's
                // [ResponseCache(NoStore, Location=None)] so it isn't cached. The
                // authenticated home response below keeps its default caching.
                Response.Headers["Cache-Control"] = "no-store,no-cache";
                Response.Headers["Pragma"] = "no-cache";

                ViewData["ReturnUrl"] = null;
                ViewData["Hero"] = PickRandomHeroKey();
                ViewData["DestinationLabel"] = null;
                return View("Welcome");
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

            // Null out ReturnUrl if it is not local, or if it points back to /welcome or /login (would redirect-loop).
            if (!Url.IsLocalUrl(ReturnUrl) || IsWelcomeOrLoginPath(ReturnUrl))
            {
                ReturnUrl = null;
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl);
            }

            // Only passive arrivals get the splash: the bare site root or a top-level area
            // landing page (e.g. "/ClinicalScheduler"). A deep link (e.g. "/ClinicalScheduler/rotation")
            // skips the interstitial and goes straight to CAS so we don't interrupt a targeted workflow.
            // In a subpath deployment the ReturnUrl carries the PathBase (e.g. "/2/ClinicalScheduler"),
            // so normalize it to an app-relative path before classifying or labeling it. The full
            // ReturnUrl is preserved in ViewData so the splash sends the user back to the right place.
            var relativeReturnUrl = StripPathBase(ReturnUrl, Request.PathBase.Value);
            if (!IsSplashTarget(relativeReturnUrl, GetAreaNames(_actionDescriptorProvider)))
            {
                return RedirectToAction(nameof(Login), new { ReturnUrl });
            }

            ViewData["ReturnUrl"] = ReturnUrl;
            ViewData["Hero"] = PickRandomHeroKey();
            ViewData["DestinationLabel"] = WelcomePageHelper.ResolveDestinationLabel(relativeReturnUrl);

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

        // internal (not private) so the redirect-loop guard is unit-testable via InternalsVisibleTo.
        internal static bool IsWelcomeOrLoginPath(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            int cut = url.IndexOfAny(['?', '#']);
            var path = cut >= 0 ? url[..cut] : url;
            path = path.TrimEnd('/');

            return path.Equals("/welcome", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/login", StringComparison.OrdinalIgnoreCase);
        }

        // Controllers under web/Areas live in the "Viper.Areas.<Area>.…" namespace. Deriving the area
        // set from controller namespaces (rather than the [Area] route value) covers every area —
        // including SPA areas whose controllers are API-only and carry no [Area] attribute — and needs
        // no hand-maintained list: add an area the usual way and it is picked up automatically.
        private const string AreaNamespacePrefix = "Viper.Areas.";

        // Cached per descriptor collection: the collection is immutable and replaced wholesale
        // (new instance) only when endpoints change, so the area set is derived once instead of
        // per anonymous /welcome request. Benign race: concurrent first requests may each compute
        // the set; last writer wins with an identical result.
        private static (ActionDescriptorCollection Source, HashSet<string> Areas)? _areaNamesCache;

        // The set of top-level area names (e.g. "Effort", "ClinicalScheduler"). Used to tell an area
        // landing page ("/Effort" → splash) apart from a deep link ("/Effort/Reports" → CAS).
        private static HashSet<string> GetAreaNames(IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            var descriptors = actionDescriptorProvider.ActionDescriptors;
            var cache = _areaNamesCache;
            if (cache == null || !ReferenceEquals(cache.Value.Source, descriptors))
            {
                var areas = descriptors.Items
                    .OfType<ControllerActionDescriptor>()
                    .Select(d => AreaFromControllerNamespace(d.ControllerTypeInfo.Namespace))
                    .Where(area => area != null)
                    .Select(area => area!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                cache = (descriptors, areas);
                _areaNamesCache = cache;
            }

            return cache.Value.Areas;
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

            int cut = url.IndexOfAny(['?', '#']);
            var path = (cut >= 0 ? url[..cut] : url).Trim('/');

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

            if (!Url.IsLocalUrl(ReturnUrl))
            {
                ReturnUrl = null;
            }

            Uri url = new(Request.GetDisplayUrl());
            string baseURl = url.GetLeftPart(UriPartial.Authority);
            string returnURL = HttpHelper.GetRootURL().Replace(baseURl, "");

            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                returnURL = ReturnUrl;
            }

            if (returnURL.StartsWith("/api"))
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
        [IgnoreAntiforgeryToken]
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
            var returnUrl = WebUtility.UrlEncode(HttpHelper.GetRootURL());
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
        /// Utility function for creating redirect URLs
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns>Compiled URL</returns>
        private static string BuildRedirectUri(string targetPath)
        {
            return HttpHelper.GetRootURL() + targetPath;
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

                    if (!Url.IsLocalUrl(returnUrl))
                    {
                        returnUrl = null;
                    }

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
