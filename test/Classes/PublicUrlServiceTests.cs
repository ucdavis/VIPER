using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Viper.Classes;

namespace Viper.test.Classes;

/// <summary>
/// The canonical public origin must come from configuration in deployed environments so a
/// forged Host header cannot influence a CAS callback. Development keeps the request-derived
/// fallback because the local port is dynamic.
/// </summary>
public class PublicUrlServiceTests
{
    private const string TestBaseUrl = "https://secure-test.vetmed.ucdavis.edu/2";
    private const string ProductionBaseUrl = "https://viper.vetmed.ucdavis.edu/2";

    [Fact]
    public void BaseUrl_ConfiguredOriginWins_OverForgedHostHeader()
    {
        var service = CreateService(TestBaseUrl, host: "attacker.example", pathBase: "/2");

        Assert.Equal(TestBaseUrl, service.BaseUrl);
    }

    [Fact]
    public void BuildUrl_ConfiguredOriginWins_OverForgedHostHeader()
    {
        var service = CreateService(ProductionBaseUrl, host: "attacker.example", pathBase: "/2");

        Assert.Equal($"{ProductionBaseUrl}/CasLogin", service.BuildUrl("/CasLogin"));
        Assert.DoesNotContain("attacker.example", service.BuildUrl("/CasLogin"), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://viper.vetmed.ucdavis.edu/2/", "https://viper.vetmed.ucdavis.edu/2")]
    [InlineData("  https://viper.vetmed.ucdavis.edu/2  ", "https://viper.vetmed.ucdavis.edu/2")]
    [InlineData("https://viper.vetmed.ucdavis.edu/", "https://viper.vetmed.ucdavis.edu")]
    public void NormalizeBaseUrl_TrimsWhitespaceAndTrailingSlash(string configured, string expected)
    {
        Assert.Equal(expected, PublicUrlService.NormalizeBaseUrl(configured));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeBaseUrl_BlankIsNull(string? configured)
    {
        Assert.Null(PublicUrlService.NormalizeBaseUrl(configured));
    }

    [Fact]
    public void BuildUrl_AddsSeparator_WhenPathHasNoLeadingSlash()
    {
        var service = CreateService(TestBaseUrl, host: "secure-test.vetmed.ucdavis.edu", pathBase: "/2");

        Assert.Equal($"{TestBaseUrl}/CasLogin", service.BuildUrl("CasLogin"));
    }

    [Fact]
    public void BuildUrl_EmptyPath_ReturnsBaseUrl()
    {
        var service = CreateService(TestBaseUrl, host: "secure-test.vetmed.ucdavis.edu", pathBase: "/2");

        Assert.Equal(TestBaseUrl, service.BuildUrl(string.Empty));
    }

    [Fact]
    public void BaseUrl_Unconfigured_FallsBackToRequestIncludingPathBase()
    {
        // Development only: no PublicBaseUrl set, so the origin comes from the request.
        var service = CreateService(configured: null, host: "localhost:7157", pathBase: "/2");

        Assert.Equal("https://localhost:7157/2", service.BaseUrl);
    }

    [Fact]
    public void BaseUrl_Unconfigured_NoPathBase_ReturnsOriginOnly()
    {
        var service = CreateService(configured: null, host: "localhost:7157", pathBase: string.Empty);

        Assert.Equal("https://localhost:7157", service.BaseUrl);
    }

    [Fact]
    public void BaseUrl_Unconfigured_NoRequest_FallsBackToLocalDevelopmentOrigin()
    {
        // Development background work (Hangfire email) has no request to derive from. Deployed
        // environments never reach this because startup validation requires the configured value.
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.ReturnsNull();
        var service = new PublicUrlService(Options.Create(new PublicUrlOptions()), accessor);

        string expectedPort = Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT") ?? "7157";

        Assert.Equal($"https://localhost:{expectedPort}", service.BaseUrl);
    }

    [Fact]
    public void BaseUrl_Configured_NoRequest_StillUsesTheCanonicalOrigin()
    {
        // The email path must not pick up the local development origin in a deployed environment.
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.ReturnsNull();
        var service = new PublicUrlService(Options.Create(new PublicUrlOptions { PublicBaseUrl = ProductionBaseUrl }), accessor);

        Assert.Equal(ProductionBaseUrl, service.BaseUrl);
    }

    #region Startup validation

    [Theory]
    [InlineData(TestBaseUrl)]
    [InlineData(ProductionBaseUrl)]
    [InlineData("https://viper.vetmed.ucdavis.edu")]
    public void Validate_AcceptsCanonicalDeployedUrls(string configured)
    {
        Assert.True(PublicUrlOptionsValidator.ValidateBaseUrl(configured, isDevelopment: false).Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingOutsideDevelopment_FailsStartup(string? configured)
    {
        var result = PublicUrlOptionsValidator.ValidateBaseUrl(configured, isDevelopment: false);

        Assert.True(result.Failed);
        Assert.Contains("Application:PublicBaseUrl", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_MissingInDevelopment_Succeeds()
    {
        // Development derives the origin from the request so dynamic local ports keep working.
        Assert.True(PublicUrlOptionsValidator.ValidateBaseUrl(null, isDevelopment: true).Succeeded);
    }

    [Fact]
    public void Validate_HttpOutsideDevelopment_Fails()
    {
        Assert.True(PublicUrlOptionsValidator.ValidateBaseUrl("http://viper.vetmed.ucdavis.edu/2", isDevelopment: false).Failed);
    }

    [Fact]
    public void Validate_HttpInDevelopment_Succeeds()
    {
        Assert.True(PublicUrlOptionsValidator.ValidateBaseUrl("http://localhost:5000", isDevelopment: true).Succeeded);
    }

    [Theory]
    [InlineData("/2")]
    [InlineData("viper.vetmed.ucdavis.edu/2")]
    [InlineData("https://user:pass@viper.vetmed.ucdavis.edu/2")]
    [InlineData("https://viper.vetmed.ucdavis.edu/2?next=x")]
    [InlineData("https://viper.vetmed.ucdavis.edu/2#frag")]
    public void Validate_RejectsMalformedOrUnsafeValues(string configured)
    {
        Assert.True(PublicUrlOptionsValidator.ValidateBaseUrl(configured, isDevelopment: false).Failed);
    }

    #endregion

    private static PublicUrlService CreateService(string? configured, string host, string pathBase)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString(host);
        context.Request.PathBase = new PathString(pathBase);
        context.Request.Path = new PathString("/CasLogin");

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        return new PublicUrlService(Options.Create(new PublicUrlOptions { PublicBaseUrl = configured }), accessor);
    }
}
