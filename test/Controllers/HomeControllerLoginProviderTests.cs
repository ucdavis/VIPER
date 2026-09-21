using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using NSubstitute;
using Viper.Classes.SQLContext;
using Viper.Classes;
using Viper.Controllers;
using Web.Authorization;

namespace Viper.test.Controllers;

/// <summary>
/// HomeController's sign-in and sign-out routing for whichever single provider is enabled.
/// </summary>
public sealed class HomeControllerLoginProviderTests
{
    private static HomeController CreateController(LoginProviders enabledProviders, bool authenticated = false)
    {
        var controller = new HomeController(
            Substitute.For<IHttpClientFactory>(),
            Options.Create(new CasSettings { CasBaseUrl = "https://cas.example.edu/" }),
            new PublicUrlService(
                Options.Create(new PublicUrlOptions { PublicBaseUrl = "https://viper.example.edu/2" }),
                Substitute.For<IHttpContextAccessor>()),
            Options.Create(new AuthenticationSettings { EnabledProviders = enabledProviders }),
            Substitute.For<AAUDContext>(),
            Substitute.For<RAPSContext>(),
            Substitute.For<VIPERContext>());

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

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        // View() resolves ITempDataDictionaryFactory from DI unless TempData is already set.
        controller.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());

        // Mirror framework semantics: rooted "/..." is local, protocol-relative "//" and "/\" are not.
        var url = Substitute.For<IUrlHelper>();
        url.IsLocalUrl(Arg.Any<string?>()).Returns(ci =>
        {
            var candidate = ci.Arg<string?>();
            return !string.IsNullOrEmpty(candidate)
                && candidate.StartsWith('/')
                && !candidate.StartsWith("//")
                && !candidate.StartsWith("/\\");
        });
        url.Content("~/").Returns("/");
        controller.Url = url;

        return controller;
    }

    [Fact]
    public void Login_CasOnly_RedirectsToCas()
    {
        var controller = CreateController(LoginProviders.Cas);

        var result = Assert.IsType<RedirectResult>(controller.Login());

        Assert.StartsWith("https://cas.example.edu/login?service=", result.Url);
    }

    [Fact]
    public void Login_EntraIdOnly_RedirectsToEntraLogin()
    {
        var controller = CreateController(LoginProviders.EntraId);

        var result = Assert.IsType<RedirectToActionResult>(controller.Login());

        Assert.Equal(nameof(HomeController.EntraLogin), result.ActionName);
    }

    [Fact]
    public void EntraLogin_WhenDisabled_ReturnsNotFound()
    {
        var controller = CreateController(LoginProviders.Cas);

        Assert.IsType<NotFoundResult>(controller.EntraLogin());
    }

    [Fact]
    public void EntraLogin_WhenEnabled_ChallengesEntraScheme()
    {
        var controller = CreateController(LoginProviders.EntraId);

        var result = Assert.IsType<ChallengeResult>(controller.EntraLogin("/Effort"));

        Assert.Equal(EntraIdClaimMapper.AuthenticationScheme, Assert.Single(result.AuthenticationSchemes));
        Assert.Equal("/Effort", result.Properties?.RedirectUri);
    }

    // With two Entra accounts signed in, the tenant session is reused silently and the second is
    // unreachable. The picker is how a user switches, so a deliberate switch has to ask for it.
    [Fact]
    public void EntraLogin_SelectAccount_AsksEntraForTheAccountPicker()
    {
        var controller = CreateController(LoginProviders.EntraId);

        var result = Assert.IsType<ChallengeResult>(controller.EntraLogin("/Effort", selectAccount: true));

        Assert.Equal(
            "select_account",
            result.Properties?.GetParameter<string>(OpenIdConnectParameterNames.Prompt));
    }

    // The passive redirect out of a protected page must stay silent, or every SSO hop grows a
    // picker click.
    [Fact]
    public void EntraLogin_Default_SendsNoPrompt()
    {
        var controller = CreateController(LoginProviders.EntraId);

        var result = Assert.IsType<ChallengeResult>(controller.EntraLogin("/Effort"));

        Assert.Null(result.Properties?.GetParameter<string>(OpenIdConnectParameterNames.Prompt));
    }

    // Forcing the picker here would charge the single-account majority a click on every sign-in,
    // when Entra already raises its own picker for the ambiguous case.
    [Fact]
    public void Login_NeverRequestsTheAccountPicker()
    {
        var controller = CreateController(LoginProviders.EntraId);

        var result = Assert.IsType<RedirectToActionResult>(controller.Login());

        Assert.Equal(nameof(HomeController.EntraLogin), result.ActionName);
        Assert.False(result.RouteValues?.ContainsKey("selectAccount"));
    }

    // The OIDC handler follows RedirectUri wherever it points after sign-in.
    [Theory]
    [InlineData("https://evil.example.com/")]
    [InlineData("//evil.example.com/")]
    public void EntraLogin_OffSiteReturnUrl_FallsBackToAppRoot(string returnUrl)
    {
        var controller = CreateController(LoginProviders.EntraId);

        var result = Assert.IsType<ChallengeResult>(controller.EntraLogin(returnUrl));

        Assert.Equal("/", result.Properties?.RedirectUri);
    }

    // The /api guard is shared by both providers, so it must hold on the Entra path too.
    [Theory]
    [InlineData("/api/secret")]
    [InlineData("~/api/secret")]
    public void EntraLogin_RejectsApiReturnUrl_WithUnauthorized(string returnUrl)
    {
        var controller = CreateController(LoginProviders.EntraId);

        Assert.IsType<UnauthorizedResult>(controller.EntraLogin(returnUrl));
    }

    [Theory]
    [InlineData(LoginProviders.Cas, "/api/secret")]
    [InlineData(LoginProviders.EntraId, "/api/secret")]
    [InlineData(LoginProviders.EntraId, "~/api/secret")]
    public void Login_RejectsApiReturnUrl_ForEitherProvider(LoginProviders enabled, string returnUrl)
    {
        var controller = CreateController(enabled);

        Assert.IsType<UnauthorizedResult>(controller.Login(returnUrl));
    }

    [Fact]
    public async Task CasLogin_WhenCasDisabled_ReturnsNotFound()
    {
        var controller = CreateController(LoginProviders.EntraId);

        Assert.IsType<NotFoundResult>(await controller.CasLogin(ticket: "ST-1"));
    }

    // Logout is the only action that reaches the authentication stack, so it needs an
    // IAuthenticationService in the container that the other tests can do without.
    private static HomeController ArrangeForLogout(
        LoginProviders enabled,
        string authenticationMethod,
        string? loginHint = null)
    {
        var controller = CreateController(enabled, authenticated: true);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "tester"),
            new(ClaimTypes.AuthenticationMethod, authenticationMethod)
        };

        if (loginHint != null)
        {
            claims.Add(new Claim(EntraIdClaimMapper.LoginHintClaimType, loginHint));
        }

        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IAuthenticationService>());
        controller.HttpContext.RequestServices = services.BuildServiceProvider();
        controller.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(claims, authenticationType: "TestAuth"));

        return controller;
    }

    // CustomAntiforgeryFilter only validates tokens on unsafe methods, so dropping the verb
    // constraint would silently remove the CSRF protection along with it.
    [Fact]
    public void Logout_IsPostOnly()
    {
        var method = typeof(HomeController).GetMethod(nameof(HomeController.Logout));

        Assert.NotNull(method);
        Assert.Single(method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false));
    }

    [Fact]
    public async Task Logout_EntraUser_WhileEntraEnabled_SignsOutOfEntraScheme()
    {
        var controller = ArrangeForLogout(LoginProviders.EntraId, EntraIdClaimMapper.AuthenticationMethod);

        var result = Assert.IsType<SignOutResult>(await controller.Logout());

        Assert.Equal(EntraIdClaimMapper.AuthenticationScheme, Assert.Single(result.AuthenticationSchemes));
    }

    // SaveTokens is off, so no id_token_hint is ever sent and this is the only handle sign-out
    // has on which account to end. Without it Entra asks the user to pick.
    [Fact]
    public async Task Logout_EntraUser_WithLoginHint_CarriesItToTheSignOutRequest()
    {
        var controller = ArrangeForLogout(
            LoginProviders.EntraId, EntraIdClaimMapper.AuthenticationMethod, loginHint: "hint-a");

        var result = Assert.IsType<SignOutResult>(await controller.Logout());

        Assert.Equal("hint-a", result.Properties?.Items[EntraIdClaimMapper.LogoutHintPropertyKey]);
    }

    // Sessions predating the optional claim have no hint, and a blank logout_hint is worse than
    // none: sign-out must degrade to exactly the URL it sent before.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Logout_EntraUser_WithoutLoginHint_OmitsIt(string? loginHint)
    {
        var controller = ArrangeForLogout(
            LoginProviders.EntraId, EntraIdClaimMapper.AuthenticationMethod, loginHint);

        var result = Assert.IsType<SignOutResult>(await controller.Logout());

        Assert.False(
            result.Properties?.Items.ContainsKey(EntraIdClaimMapper.LogoutHintPropertyKey));
    }

    // An Entra cookie outlives the provider being switched off (12h expiry), e.g. reverting a
    // cutover back to Cas. Falling through to the CAS logout redirect would send a user who never
    // had a CAS session to CAS's logout page.
    [Fact]
    public async Task Logout_EntraUser_AfterEntraDisabled_RedirectsLocallyNotToCas()
    {
        var controller = ArrangeForLogout(LoginProviders.Cas, EntraIdClaimMapper.AuthenticationMethod);

        var result = Assert.IsType<LocalRedirectResult>(await controller.Logout());

        Assert.Equal("~/", result.Url);
    }

    [Fact]
    public async Task Logout_CasUser_StillRedirectsToCasLogout()
    {
        var controller = ArrangeForLogout(LoginProviders.Cas, "CAS");

        var result = Assert.IsType<RedirectResult>(await controller.Logout());

        Assert.StartsWith("https://cas.example.edu/logout?service=", result.Url);
    }
}
