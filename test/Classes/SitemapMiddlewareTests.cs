using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Viper.Classes;

namespace Viper.test.Classes;

/// <summary>
/// The sitemap reflects over every controller action. PermissionAttribute derives from
/// AuthorizeAttribute, so an action carrying both matched AuthorizeAttribute twice and the
/// singular GetCustomAttribute threw AmbiguousMatchException. The catch swallowed it and the
/// endpoint fell through to a 404 in every environment.
/// </summary>
public class SitemapMiddlewareTests
{
    [Fact]
    public async Task SitemapXml_Returns200Xml_NotAFallThrough()
    {
        bool nextCalled = false;
        var middleware = new SitemapMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<SitemapMiddleware>.Instance);

        var context = BuildContext("/sitemap.xml");
        using var body = new MemoryStream();
        context.Response.Body = body;

        await middleware.Invoke(context);

        Assert.False(nextCalled, "generation failed and fell through to the pipeline");
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("application/xml", context.Response.ContentType);

        string xml = Encoding.UTF8.GetString(body.ToArray());
        Assert.StartsWith("<urlset", xml, StringComparison.Ordinal);
        Assert.EndsWith("</urlset>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SitemapXml_IncludesAnonymousActions()
    {
        var middleware = new SitemapMiddleware(_ => Task.CompletedTask, NullLogger<SitemapMiddleware>.Instance);

        var context = BuildContext("/sitemap.xml");
        using var body = new MemoryStream();
        context.Response.Body = body;

        await middleware.Invoke(context);

        // An empty <urlset/> would mean the reflection walk bailed out without throwing.
        string xml = Encoding.UTF8.GetString(body.ToArray());
        Assert.Contains("<loc>", xml, StringComparison.Ordinal);

        // HomeController.Index is [AllowAnonymous] with no gate, so it belongs in a public sitemap.
        Assert.Contains("/home/index</loc>", xml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SitemapXml_ExcludesPermissionGatedActions()
    {
        var middleware = new SitemapMiddleware(_ => Task.CompletedTask, NullLogger<SitemapMiddleware>.Instance);

        var context = BuildContext("/sitemap.xml");
        using var body = new MemoryStream();
        context.Response.Body = body;

        await middleware.Invoke(context);

        // EmulateUser carries [Authorize] and [Permission], the pairing that used to throw.
        // It must stay out of a public sitemap.
        string xml = Encoding.UTF8.GetString(body.ToArray());
        Assert.DoesNotContain("emulateuser", xml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OtherPaths_FallThroughUntouched()
    {
        bool nextCalled = false;
        var middleware = new SitemapMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<SitemapMiddleware>.Instance);

        await middleware.Invoke(BuildContext("/Directory"));

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext BuildContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost:7157");
        context.Request.Path = new PathString(path);
        return context;
    }
}
