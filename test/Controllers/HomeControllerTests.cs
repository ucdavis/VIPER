using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    public HomeControllerTests()
    {
        _controller = new HomeController(
            Substitute.For<IHttpClientFactory>(),
            Options.Create(new CasSettings { CasBaseUrl = "https://cas.example.edu/" }),
            Substitute.For<AAUDContext>(),
            Substitute.For<RAPSContext>(),
            Substitute.For<VIPERContext>());
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

    [Fact]
    public void Index_Anonymous_RendersWelcomeWithNoStore()
    {
        Arrange(authenticated: false);

        var result = _controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Welcome", view.ViewName);
        Assert.Equal("no-store,no-cache", _controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public void Welcome_Anonymous_KeepsLocalReturnUrl()
    {
        Arrange(authenticated: false);

        var result = _controller.Welcome("/RAPS/Roles");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Welcome", view.ViewName);
        Assert.Equal("/RAPS/Roles", view.ViewData["ReturnUrl"]);
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

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData(null)]
    public void Welcome_Authenticated_RedirectsToRootWhenReturnUrlInvalidOrMissing(string? returnUrl)
    {
        Arrange(authenticated: true);

        var result = _controller.Welcome(returnUrl);

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
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
