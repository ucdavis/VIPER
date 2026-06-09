using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Viper.Classes.SQLContext;
using Viper.Controllers;
using Web.Authorization;

namespace Viper.test.Controllers;

/// <summary>
/// Unit tests for HomeController's anonymous landing / login flow, focused on the
/// open-redirect protections and redirect-loop guard added with the welcome page.
/// </summary>
public sealed class HomeControllerTests
{
    private readonly HomeController _controller;

    // Real area controllers, one per area. GetAreaNames() derives the area set from the
    // "Viper.Areas.<Area>.…" controller namespace, so these expose ClinicalScheduler/Effort/RAPS/CTS.
    // Effort is intentionally an API-only area (its controllers carry no [Area]) — the case the
    // namespace-based derivation fixes versus the old [Area] route-value lookup.
    private static readonly Type[] _areaControllerTypes =
    {
        typeof(Viper.Areas.ClinicalScheduler.Controllers.CliniciansController),
        typeof(Viper.Areas.Effort.Controllers.ReportsController),
        typeof(Viper.Areas.RAPS.Controllers.RAPSController),
        typeof(Viper.Areas.CTS.Controllers.CTSController),
    };

    // "/ClinicalScheduler" is a splash-eligible area landing page; "/ClinicalScheduler/rotation" is a deep link.
    private static readonly string[] _areas = { "ClinicalScheduler", "Effort", "RAPS", "CTS" };

    public HomeControllerTests()
    {
        var actionProvider = Substitute.For<IActionDescriptorCollectionProvider>();
        var descriptors = _areaControllerTypes
            .Select(t => new ControllerActionDescriptor { ControllerTypeInfo = t.GetTypeInfo() })
            .ToList();
        actionProvider.ActionDescriptors.Returns(new ActionDescriptorCollection(descriptors, version: 1));

        _controller = new HomeController(
            Substitute.For<IHttpClientFactory>(),
            Options.Create(new CasSettings { CasBaseUrl = "https://cas.example.edu/" }),
            Substitute.For<AAUDContext>(),
            Substitute.For<RAPSContext>(),
            Substitute.For<VIPERContext>(),
            actionProvider);
    }

    /// <summary>
    /// Wires up a controller context with the requested auth state and a URL helper whose
    /// IsLocalUrl mirrors framework semantics (local = rooted path, not protocol-relative).
    /// </summary>
    private void Arrange(bool authenticated)
    {
        var identity = authenticated
            ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "tester") }, authenticationType: "TestAuth")
            : new ClaimsIdentity();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("viper.test");
        httpContext.Request.Path = "/login";

        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        // View() resolves ITempDataDictionaryFactory from DI unless TempData is already set.
        _controller.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());

        var url = Substitute.For<IUrlHelper>();
        url.IsLocalUrl(Arg.Any<string?>()).Returns(ci =>
        {
            var candidate = ci.Arg<string?>();
            if (string.IsNullOrEmpty(candidate))
            {
                return false;
            }

            // Mirror framework semantics: rooted "/..." and app-relative "~/..." are
            // local, but protocol-relative ("//"), backslash ("/\") and their "~/"
            // variants are not.
            if (candidate.StartsWith('/'))
            {
                return !candidate.StartsWith("//") && !candidate.StartsWith("/\\");
            }

            return candidate.StartsWith("~/")
                && !candidate.StartsWith("~//")
                && !candidate.StartsWith("~/\\");
        });
        _controller.Url = url;
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("/welcome", true)]
    [InlineData("/Welcome", true)]
    [InlineData("/welcome/", true)]
    [InlineData("/login", true)]
    [InlineData("/LOGIN?ReturnUrl=/x", true)]
    [InlineData("/welcome#frag", true)]
    [InlineData("/RAPS/Roles", false)]
    [InlineData("/welcomepage", false)]
    public void IsWelcomeOrLoginPath_DetectsLoopTargets(string? url, bool expected)
    {
        Assert.Equal(expected, HomeController.IsWelcomeOrLoginPath(url));
    }

    // Splash appears only for the front door (an empty return path or the bare site root) and for a
    // single-segment area landing page. Anything deeper, or a single segment that is not a registered
    // area, is treated as a deep link.
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("/", true)]
    [InlineData("/ClinicalScheduler", true)]
    [InlineData("/clinicalscheduler", true)] // area match is case-insensitive
    [InlineData("/Effort/", true)] // trailing slash on an area root still counts
    [InlineData("/Effort?tab=1", true)] // query string is ignored
    [InlineData("/ClinicalScheduler/rotation", false)] // deep link
    [InlineData("/CTS/epa", false)] // CTS is an area, but this is a deep link
    [InlineData("/MyPermissions", false)] // single segment, not an area
    [InlineData("/cahfs", false)] // not a registered MVC area
    public void IsSplashTarget_ClassifiesPassiveLandingsVsDeepLinks(string? url, bool expected)
    {
        var areas = new HashSet<string>(_areas, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(expected, HomeController.IsSplashTarget(url, areas));
    }

    // In a subpath deployment (PathBase "/2") the ReturnUrl is prefixed with the base. StripPathBase
    // removes it on a segment boundary so the splash classifier sees an app-relative path, while leaving
    // unrelated paths (and the no-base dev case) untouched.
    [Theory]
    [InlineData(null, "/2", null)]
    [InlineData("", "/2", "")]
    [InlineData("/2/ClinicalScheduler", "/2", "/ClinicalScheduler")]
    [InlineData("/2/ClinicalScheduler/rotation", "/2", "/ClinicalScheduler/rotation")]
    [InlineData("/2", "/2", "")] // app root under the subpath
    [InlineData("/2/", "/2", "/")]
    [InlineData("/2?tab=1", "/2", "?tab=1")] // query is preserved for the classifier to strip
    [InlineData("/22/x", "/2", "/22/x")] // segment boundary: "/2" must not strip from "/22"
    [InlineData("/ClinicalScheduler", "", "/ClinicalScheduler")] // no base configured (dev)
    [InlineData("/ClinicalScheduler", null, "/ClinicalScheduler")]
    public void StripPathBase_RemovesBaseOnSegmentBoundary(string? url, string? pathBase, string? expected)
    {
        Assert.Equal(expected, HomeController.StripPathBase(url, pathBase));
    }

    // Area names come from controller namespaces (Viper.Areas.<Area>.…), so API-only areas with no
    // [Area] attribute are still recognized. Non-area namespaces and near-matches resolve to null.
    [Theory]
    [InlineData("Viper.Areas.Effort.Controllers", "Effort")]
    [InlineData("Viper.Areas.ClinicalScheduler.Controllers.SomethingV2", "ClinicalScheduler")]
    [InlineData("Viper.Areas.CMS", "CMS")]
    [InlineData("Viper.Controllers", null)] // not an area
    [InlineData("Viper.Areas", null)] // no area segment
    [InlineData("Viper.AreasButNotReally.X", null)] // prefix must end on a namespace boundary
    [InlineData(null, null)]
    public void AreaFromControllerNamespace_ExtractsAreaSegment(string? ns, string? expected)
    {
        Assert.Equal(expected, HomeController.AreaFromControllerNamespace(ns));
    }

    [Fact]
    public void Index_Anonymous_RendersWelcomeWithNoStore()
    {
        Arrange(authenticated: false);

        var result = _controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Welcome", view.ViewName);
        Assert.Equal("no-store,no-cache", _controller.Response.Headers.CacheControl.ToString());
    }

    // An area landing page ("/ClinicalScheduler") is a passive arrival, so it still gets the splash
    // with its ReturnUrl preserved.
    [Fact]
    public void Welcome_Anonymous_AreaLanding_RendersSplashWithReturnUrl()
    {
        Arrange(authenticated: false);

        var result = _controller.Welcome("/ClinicalScheduler");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Welcome", view.ViewName);
        Assert.Equal("/ClinicalScheduler", view.ViewData["ReturnUrl"]);
    }

    // Effort is an API-only area (no [Area] MVC controller); its landing page must still splash.
    // Regression guard for the namespace-based area derivation that replaced the [Area] route-value lookup.
    [Fact]
    public void Welcome_Anonymous_ApiOnlyAreaLanding_RendersSplash()
    {
        Arrange(authenticated: false);

        var result = _controller.Welcome("/Effort");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Welcome", view.ViewName);
        Assert.Equal("/Effort", view.ViewData["ReturnUrl"]);
    }

    // A deep link ("/ClinicalScheduler/rotation") skips the interstitial and goes straight to CAS
    // via the Login action, carrying the ReturnUrl so the user lands where they intended.
    [Fact]
    public void Welcome_Anonymous_DeepLink_RedirectsToCasLogin()
    {
        Arrange(authenticated: false);

        var result = _controller.Welcome("/ClinicalScheduler/rotation");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(HomeController.Login), redirect.ActionName);
        Assert.Equal("/ClinicalScheduler/rotation", redirect.RouteValues?["ReturnUrl"]);
    }

    // Under a subpath deployment (PathBase "/2") the area landing page arrives as "/2/ClinicalScheduler".
    // The base is stripped for classification so it still gets the splash, with the friendly area label
    // resolved and the full (base-prefixed) ReturnUrl preserved for the round trip.
    [Fact]
    public void Welcome_Anonymous_SubpathAreaLanding_RendersSplash()
    {
        Arrange(authenticated: false);
        _controller.HttpContext.Request.PathBase = "/2";

        var result = _controller.Welcome("/2/ClinicalScheduler");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Welcome", view.ViewName);
        Assert.Equal("/2/ClinicalScheduler", view.ViewData["ReturnUrl"]);
        Assert.Equal("Clinical Scheduler", view.ViewData["DestinationLabel"]);
    }

    // A subpath deep link ("/2/ClinicalScheduler/rotation") still bypasses the splash and goes to CAS,
    // carrying the full base-prefixed ReturnUrl.
    [Fact]
    public void Welcome_Anonymous_SubpathDeepLink_RedirectsToCasLogin()
    {
        Arrange(authenticated: false);
        _controller.HttpContext.Request.PathBase = "/2";

        var result = _controller.Welcome("/2/ClinicalScheduler/rotation");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(HomeController.Login), redirect.ActionName);
        Assert.Equal("/2/ClinicalScheduler/rotation", redirect.RouteValues?["ReturnUrl"]);
    }

    // Under a subpath deployment the loop targets arrive base-prefixed ("/2/welcome", "/2/login").
    // The base is stripped before the loop-guard so they are still caught: the splash renders with a
    // null ReturnUrl rather than bouncing back out to CAS. Regression guard for the pre-strip loop-guard.
    [Theory]
    [InlineData("/2/welcome")]
    [InlineData("/2/login")]
    [InlineData("/2/Welcome/")]
    public void Welcome_Anonymous_SubpathLoopTarget_DropsReturnUrl(string returnUrl)
    {
        Arrange(authenticated: false);
        _controller.HttpContext.Request.PathBase = "/2";

        var result = _controller.Welcome(returnUrl);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Welcome", view.ViewName);
        Assert.Null(view.ViewData["ReturnUrl"]);
    }

    // Authenticated welcome with no ReturnUrl under a subpath deployment redirects to "~/" so the app
    // root keeps its PathBase ("/2/") instead of escaping to the domain root. Regression guard for the
    // bare "/" that sent logged-in users out to the legacy site.
    [Fact]
    public void Welcome_Authenticated_NoReturnUrl_RedirectsToAppRelativeRoot()
    {
        Arrange(authenticated: true);
        _controller.HttpContext.Request.PathBase = "/2";

        var result = _controller.Welcome();

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("~/", redirect.Url);
    }

    // Anonymous users still get the Welcome view, but any ReturnUrl that is non-local
    // (open redirect) or points back at /welcome|/login (redirect loop) is dropped.
    [Theory]
    [InlineData("https://evil.com/phish")]
    [InlineData("//evil.com")]
    [InlineData("/welcome")]
    [InlineData("/login")]
    [InlineData("~/welcome")] // app-relative loop targets must still be caught after normalization
    [InlineData("~/login")]
    public void Welcome_Anonymous_DropsUnsafeReturnUrl(string returnUrl)
    {
        Arrange(authenticated: false);

        var result = _controller.Welcome(returnUrl);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Null(view.ViewData["ReturnUrl"]);
    }

    [Fact]
    public void Welcome_Authenticated_RedirectsToLocalReturnUrl()
    {
        Arrange(authenticated: true);

        var result = _controller.Welcome("/Effort/Foo");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/Effort/Foo", redirect.Url);
    }

    // App root is "~/" (not "/") so a subpath deployment keeps its PathBase ("/2/") rather than
    // escaping to the domain root (the legacy site).
    [Theory]
    [InlineData("https://evil.com")]
    [InlineData(null)]
    public void Welcome_Authenticated_RedirectsToRootWhenReturnUrlInvalidOrMissing(string? returnUrl)
    {
        Arrange(authenticated: true);

        var result = _controller.Welcome(returnUrl);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("~/", redirect.Url);
    }

    [Theory]
    [InlineData("/api/secret")]
    [InlineData("~/api/secret")] // app-relative form must not bypass the /api guard
    public void Login_RejectsApiReturnUrl_WithUnauthorized(string returnUrl)
    {
        Arrange(authenticated: false);

        var result = _controller.Login(returnUrl);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // Under a subpath deployment the /api ReturnUrl arrives base-prefixed ("/2/api/..."). The base is
    // stripped before the guard so it is still rejected and never forwarded to CAS. Regression guard for
    // the pre-strip /api check that a "/2/api/..." ReturnUrl slipped past.
    [Theory]
    [InlineData("/2/api/secret")]
    [InlineData("~/2/api/secret")] // app-relative + base-prefixed must not bypass the guard either
    public void Login_RejectsSubpathApiReturnUrl_WithUnauthorized(string returnUrl)
    {
        Arrange(authenticated: false);
        _controller.HttpContext.Request.PathBase = "/2";

        var result = _controller.Login(returnUrl);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Theory]
    [InlineData("https://evil.com/phish")]
    [InlineData("//evil.com")]
    public void Login_DoesNotForwardNonLocalReturnUrl(string returnUrl)
    {
        Arrange(authenticated: false);

        var result = _controller.Login(returnUrl);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.DoesNotContain("evil.com", redirect.Url, StringComparison.OrdinalIgnoreCase);
    }
}
