using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Viper.Areas.CMS.Services;

namespace Viper.test.CMS;

/// <summary>
/// Tests for the CMS download rate-limit partitioning: ZIP and single-file requests get
/// separate buckets, keyed by login when authenticated and client IP otherwise.
/// </summary>
public sealed class CmsDownloadRateLimitingTests
{
    private static DefaultHttpContext MakeContext(string? queryString = null, string? login = null, string? ip = null)
    {
        var context = new DefaultHttpContext();
        if (queryString != null)
        {
            context.Request.QueryString = new QueryString(queryString);
        }
        if (login != null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, login) }, authenticationType: "test"));
        }
        if (ip != null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        }
        return context;
    }

    [Theory]
    [InlineData("?ids=abc", true)]
    [InlineData("?ids=abc,def&fileName=x.zip", true)]
    [InlineData("?ids=", false)]
    [InlineData("?id=abc", false)]
    [InlineData("?fn=somefile.pdf", false)]
    [InlineData(null, false)]
    public void IsZipRequest_DetectsIdsParameter(string? query, bool expected)
    {
        var context = MakeContext(query);

        Assert.Equal(expected, CmsDownloadRateLimiting.IsZipRequest(context));
    }

    [Fact]
    public void PartitionKey_ZipAndFileRequests_GetSeparateBuckets()
    {
        var zipKey = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?ids=a,b", login: "rexl"));
        var fileKey = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=x.pdf", login: "rexl"));

        Assert.NotEqual(zipKey, fileKey);
        Assert.StartsWith("zip|", zipKey);
        Assert.StartsWith("file|", fileKey);
    }

    [Fact]
    public void PartitionKey_AuthenticatedUser_KeysOnLoginNotIp()
    {
        var sameUserIp1 = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=x.pdf", login: "rexl", ip: "10.0.0.1"));
        var sameUserIp2 = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=x.pdf", login: "rexl", ip: "10.0.0.2"));
        var otherUserSameIp = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=x.pdf", login: "bob", ip: "10.0.0.1"));

        Assert.Equal(sameUserIp1, sameUserIp2);
        Assert.NotEqual(sameUserIp1, otherUserSameIp);
    }

    [Fact]
    public void PartitionKey_Anonymous_KeysOnClientIp()
    {
        var ip1 = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=x.pdf", ip: "10.0.0.1"));
        var ip2 = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=x.pdf", ip: "10.0.0.2"));

        Assert.NotEqual(ip1, ip2);
        Assert.Contains("10.0.0.1", ip1);
    }

    [Fact]
    public void PartitionKey_AnonymousWithoutIp_UsesStableFallback()
    {
        var key1 = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=x.pdf"));
        var key2 = CmsDownloadRateLimiting.GetPartitionKey(MakeContext("?fn=y.pdf"));

        Assert.Equal("file|unknown", key1);
        Assert.Equal(key1, key2);
    }
}
