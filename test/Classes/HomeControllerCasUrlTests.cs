using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Controllers;
using Web.Authorization;

namespace Viper.test.Classes;

/// <summary>
/// CAS service callbacks must be built from the configured canonical origin, never from the
/// request Host. Login covers the shared BuildRedirectUri helper that CasLogin's ticket
/// validation also uses.
/// </summary>
public class HomeControllerCasUrlTests
{
    private const string CasBaseUrl = "https://ssodev.ucdavis.edu/cas/";
    private const string PublicBaseUrl = "https://secure-test.vetmed.ucdavis.edu/2";
    private const string ForgedHost = "attacker.example";

    [Fact]
    public void Login_BuildsServiceFromConfiguredOrigin_NotHostHeader()
    {
        var controller = CreateController(ForgedHost, pathBase: "/2");

        var result = Assert.IsType<RedirectResult>(controller.Login());

        Assert.DoesNotContain(ForgedHost, result.Url, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith($"{PublicBaseUrl}/CasLogin?", ServiceParameter(result.Url), StringComparison.Ordinal);
    }

    [Fact]
    public void Login_DefaultReturnUrl_PreservesPathBase()
    {
        var controller = CreateController(ForgedHost, pathBase: "/2");

        var result = Assert.IsType<RedirectResult>(controller.Login());

        // ReturnUrl is encoded inside the service value, which is then encoded again for CAS,
        // so one decode leaves the inner encoding intact.
        Assert.Equal($"{PublicBaseUrl}/CasLogin?ReturnUrl={WebUtility.UrlEncode("/2")}", ServiceParameter(result.Url));
    }

    [Fact]
    public void Login_NoPathBase_DefaultsToEmptyReturnUrl()
    {
        var controller = CreateController("localhost:7157", pathBase: string.Empty);

        var result = Assert.IsType<RedirectResult>(controller.Login());

        Assert.Equal($"{PublicBaseUrl}/CasLogin?ReturnUrl=", ServiceParameter(result.Url));
    }

    [Fact]
    public void Login_ExplicitReturnUrl_IsPreserved()
    {
        var controller = CreateController(ForgedHost, pathBase: "/2");

        var result = Assert.IsType<RedirectResult>(controller.Login("/2/Students/StudentClassYear"));

        Assert.Equal(
            $"{PublicBaseUrl}/CasLogin?ReturnUrl={WebUtility.UrlEncode("/2/Students/StudentClassYear")}",
            ServiceParameter(result.Url));
    }

    [Fact]
    public void Login_ApiReturnUrlUnderPathBase_ReturnsUnauthorized()
    {
        // The SPAs send ReturnUrl already prefixed with the deployed PathBase, so without
        // stripping it the API guard never fired on TEST/PROD and an API caller got a CAS
        // HTML redirect instead of a 401.
        var controller = CreateController("secure-test.vetmed.ucdavis.edu", pathBase: "/2");

        Assert.IsType<UnauthorizedResult>(controller.Login("/2/api/students/dvm"));
    }

    [Fact]
    public void Login_ApiReturnUrlWithoutPathBase_ReturnsUnauthorized()
    {
        var controller = CreateController("localhost:7157", pathBase: string.Empty);

        Assert.IsType<UnauthorizedResult>(controller.Login("/api/students/dvm"));
    }

    [Fact]
    public async Task Logout_BuildsServiceFromConfiguredOrigin_NotHostHeader()
    {
        var controller = CreateController(ForgedHost, pathBase: "/2");

        var result = Assert.IsType<RedirectResult>(await controller.Logout());

        Assert.DoesNotContain(ForgedHost, result.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Equal($"{CasBaseUrl}logout?service={WebUtility.UrlEncode(PublicBaseUrl)}", result.Url);
    }

    /// <summary>
    /// Pulls the decoded CAS service parameter out of the redirect so assertions read as URLs
    /// rather than percent-encoded soup.
    /// </summary>
    private static string ServiceParameter(string redirectUrl)
    {
        const string marker = "service=";
        int start = redirectUrl.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"No service parameter in '{redirectUrl}'.");

        return WebUtility.UrlDecode(redirectUrl[(start + marker.Length)..]);
    }

    private static HomeController CreateController(string host, string pathBase)
    {
        var publicUrl = new PublicUrlService(
            Options.Create(new PublicUrlOptions { PublicBaseUrl = PublicBaseUrl }),
            Substitute.For<IHttpContextAccessor>());

        var controller = new HomeController(
            Substitute.For<IHttpClientFactory>(),
            Options.Create(new CasSettings { CasBaseUrl = CasBaseUrl }),
            publicUrl,
            Substitute.For<AAUDContext>(),
            Substitute.For<RAPSContext>(),
            Substitute.For<VIPERContext>());

        var httpContext = new DefaultHttpContext
        {
            RequestServices = AuthenticationServices()
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString(host);
        httpContext.Request.PathBase = new PathString(pathBase);
        httpContext.Request.Path = new PathString("/Login");

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    // Logout signs the cookie out, which resolves IAuthenticationService from the request.
    private static IServiceProvider AuthenticationServices()
    {
        var authentication = Substitute.For<IAuthenticationService>();
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IAuthenticationService)).Returns(authentication);
        return services;
    }
}
