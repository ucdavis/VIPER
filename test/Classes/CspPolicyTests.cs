using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Viper.Classes;

namespace Viper.test.Classes;

/// <summary>Built Vue SPA responses must not permit dynamic code evaluation. See CspPolicy.</summary>
public class CspPolicyTests
{
    // Shape of the emitted header. Joonasw's CspOptions joins directives with ';' and no space.
    private const string ApplicationPolicy =
        "script-src 'self' 'nonce-abc123' 'unsafe-eval';style-src 'self' fonts.googleapis.com 'unsafe-inline';img-src 'self' data:;frame-src 'none'";

    private const string TightenedPolicy =
        "script-src 'self' 'nonce-abc123';style-src 'self' fonts.googleapis.com 'unsafe-inline';img-src 'self' data:;frame-src 'none'";

    [Fact]
    public void WithoutUnsafeEval_ApplicationPolicy_DropsOnlyTheAllowance()
    {
        Assert.Equal(TightenedPolicy, CspPolicy.WithoutUnsafeEval(ApplicationPolicy));
    }

    [Theory]
    [InlineData("script-src 'self' 'unsafe-eval'", "script-src 'self'")]
    [InlineData("script-src 'unsafe-eval' 'self'", "script-src 'self'")]
    [InlineData("script-src 'self' 'unsafe-eval' 'nonce-x'", "script-src 'self' 'nonce-x'")]
    public void WithoutUnsafeEval_HandlesEveryPositionInADirective(string policy, string expected)
    {
        Assert.Equal(expected, CspPolicy.WithoutUnsafeEval(policy));
    }

    [Theory]
    [InlineData("script-src 'unsafe-eval'", "script-src 'none'")]
    [InlineData("script-src 'unsafe-eval';img-src 'self'", "script-src 'none';img-src 'self'")]
    public void WithoutUnsafeEval_SoleSourceExpression_FallsBackToNone(string policy, string expected)
    {
        Assert.Equal(expected, CspPolicy.WithoutUnsafeEval(policy));
    }

    [Fact]
    public void WithoutUnsafeEval_ValuelessDirective_IsNotGivenASource()
    {
        Assert.Equal(
            "script-src 'self';upgrade-insecure-requests",
            CspPolicy.WithoutUnsafeEval("script-src 'self' 'unsafe-eval';upgrade-insecure-requests"));
    }

    [Fact]
    public void WithoutUnsafeEval_PolicyWithoutTheAllowance_IsUnchanged()
    {
        const string policy = "script-src 'self' 'nonce-abc123';img-src 'self'";

        Assert.Equal(policy, CspPolicy.WithoutUnsafeEval(policy));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutUnsafeEval_MissingHeader_ReturnsEmpty(string? policy)
    {
        Assert.Equal(string.Empty, CspPolicy.WithoutUnsafeEval(policy));
    }

    [Fact]
    public void TightenForBuiltSpa_RewritesThePolicyOnTheResponse()
    {
        var http = new DefaultHttpContext();
        http.Response.Headers[HeaderNames.ContentSecurityPolicy] = ApplicationPolicy;

        CspPolicy.TightenForBuiltSpa(ResponseContextFor(http));

        Assert.Equal(TightenedPolicy, http.Response.Headers[HeaderNames.ContentSecurityPolicy].ToString());
    }

    [Fact]
    public void TightenForBuiltSpa_NoPolicyOnTheResponse_AddsNoHeader()
    {
        // The CSP middleware is skipped for HealthChecks UI paths, so the header can be absent.
        var http = new DefaultHttpContext();

        CspPolicy.TightenForBuiltSpa(ResponseContextFor(http));

        Assert.False(http.Response.Headers.ContainsKey(HeaderNames.ContentSecurityPolicy));
    }

    private static StaticFileResponseContext ResponseContextFor(HttpContext http)
    {
        return new StaticFileResponseContext(http, new NotFoundFileInfo("index.html"));
    }
}
