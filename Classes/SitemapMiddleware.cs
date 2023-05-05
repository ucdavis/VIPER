// Adapted from https://dotnetthoughts.net/generate-dynamic-xml-sitemaps-in-aspnet5/
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;

namespace Viper.Classes
{
    public class SitemapMiddleware
    {
        private RequestDelegate _next;
        private string _rootUrl;
        public SitemapMiddleware(RequestDelegate next, string rootUrl)
        {
            _next = next;
            _rootUrl = rootUrl;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request?.Path.Value != null)
            {
                if (context.Request.Path.Value.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase))
                {
                    // override default root url
                    _rootUrl = HttpHelper.GetRootURL();
                    var stream = context.Response.Body;
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/xml";
                    string sitemapContent = "<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">";
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
                            string url = string.Format("{0}/{1}/{2}", _rootUrl, controller.Name.ToLower().Replace("controller", ""), method.Name.ToLower());
                            string lastMod = DateTime.UtcNow.ToString("yyyy-MM-dd").ToString();

                            if (!URLs.ContainsKey(url))
                            {
                                URLs.Add(url, lastMod);
                            }

                        }
                        foreach (var url in URLs) 
                        { 
                            sitemapContent += "<url>";
                            sitemapContent += "<loc>" + url.Key + "</loc>";
                            sitemapContent += "<lastmod>" + url.Value + "</lastmod>";
                            sitemapContent += "</url>";
                        }
                    }
                    sitemapContent += "</urlset>";
                    using (var memoryStream = new MemoryStream())
                    {
                        var bytes = Encoding.UTF8.GetBytes(sitemapContent);
                        memoryStream.Write(bytes, 0, bytes.Length);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await memoryStream.CopyToAsync(stream, bytes.Length);
                    }
                }
                else
                {
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
        public static IApplicationBuilder UseSitemapMiddleware(this IApplicationBuilder app,
            string rootUrl = "")
        {
            return app.UseMiddleware<SitemapMiddleware>(new[] { rootUrl });
        }
    }
}