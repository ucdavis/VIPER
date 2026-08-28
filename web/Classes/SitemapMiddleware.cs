// Adapted from https://dotnetthoughts.net/generate-dynamic-xml-sitemaps-in-aspnet5/

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Authorization;

namespace Viper.Classes
{
    public class SitemapMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SitemapMiddleware> _logger;
        public SitemapMiddleware(RequestDelegate next, ILogger<SitemapMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request?.Path.Value != null
                && context.Request.Path.Value.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var rootUrl = HttpHelper.GetRootURL();
                    var stream = context.Response.Body;
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/xml";
                    var sitemapContent = new StringBuilder("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                    var controllers = Assembly.GetExecutingAssembly().GetTypes()
                        .Where(type => typeof(Controller).IsAssignableFrom(type)
                        || type.Name.EndsWith("controller")).ToList();

                    foreach (var controller in controllers)
                    {
                        var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Where(method => typeof(IActionResult).IsAssignableFrom(method.ReturnType) || typeof(Task<IActionResult>).IsAssignableFrom(method.ReturnType))
                            .Distinct<MethodInfo>();

                        Dictionary<string, string> URLs = new Dictionary<string, string>();

                        foreach (var method in methods)
                        {
                            // Testing [Permission] alone covers [Authorize] too, because
                            // PermissionAttribute derives from it. That inheritance is also why these
                            // are plural lookups: a method carrying both (HomeController.EmulateUser)
                            // matches AuthorizeAttribute twice, and the singular GetCustomAttribute
                            // throws AmbiguousMatchException, which turned the sitemap into a 404.
                            bool isAnonymous = method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0
                                || method.DeclaringType?.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Length > 0;
                            bool isPermissionGated = method.GetCustomAttributes(typeof(PermissionAttribute), inherit: true).Length > 0;
                            bool isSearchExcluded = method.GetCustomAttributes(typeof(SearchExcludeAttribute), inherit: true).Length > 0
                                || method.DeclaringType?.GetCustomAttributes(typeof(SearchExcludeAttribute), inherit: true).Length > 0;

                            if (isAnonymous && !isPermissionGated && !isSearchExcluded)
                            {
                                string url = string.Format("{0}/{1}/{2}", rootUrl, controller.Name.ToLower().Replace("controller", ""), method.Name.ToLower());
                                string lastMod = DateTime.UtcNow.ToString("yyyy-MM-dd");

                                if (!URLs.ContainsKey(url))
                                {
                                    URLs.Add(url, lastMod);
                                }

                            }

                        }
                        foreach (var url in URLs)
                        {
                            sitemapContent.Append("<url>");
                            sitemapContent.Append("<loc>").Append(url.Key).Append("</loc>");
                            sitemapContent.Append("<lastmod>").Append(url.Value).Append("</lastmod>");
                            sitemapContent.Append("</url>");
                        }
                    }
                    sitemapContent.Append("</urlset>");
                    using (var memoryStream = new MemoryStream())
                    {
                        var bytes = Encoding.UTF8.GetBytes(sitemapContent.ToString());
                        await memoryStream.WriteAsync(bytes.AsMemory(), context.RequestAborted);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await memoryStream.CopyToAsync(stream, bytes.Length, context.RequestAborted);
                    }
                }
                // A disconnected client is not a generation failure. The response has
                // already started by this point, so end the request instead of running
                // the rest of the pipeline against it.
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                {
                    // Intentionally no fall-through to _next: the request is over.
                }
                // Middleware boundary: any sitemap-generation failure (DB, IO,
                // reflection, etc.) must fall through to the pipeline rather than
                // break the request. Log it: swallowing silently is what let an
                // AmbiguousMatchException turn the sitemap into a blanket 404 unnoticed.
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _logger.LogError(ex, "Sitemap generation failed; falling through to the pipeline.");
                    await _next(context);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }

    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseSitemapMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SitemapMiddleware>();
        }
    }
}
