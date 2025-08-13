using System.Text.RegularExpressions;

namespace Web;

/// <summary>
/// Helper methods for proxying requests to Vite development server
/// </summary>
internal static partial class ViteProxyHelpers
{
    // Base path for Vite assets - make configurable for maintainability
    private const string ViteAssetsBasePath = "/2/vue/assets/";

    // Regex patterns for better maintainability and testability
    private const string AssetHashPattern = @".*-[A-Za-z0-9_-]{6,}\.";
    private const string SupportedAssetExtensions = @"(js|css|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf|eot)";
    private const string VueAppRoutePattern = @"^/({0})(/.*)?$";
    private const string VueAppAssetPattern = @"^/({0})/.*\.(js|ts|css|map|vue|json)$|^/({0})\.(js|ts|css|map|vue|json)$";
    private const string DevAssetExtensions = @"(js|ts|css|map|vue|json)";

    // Regex for built asset files with Vite hashes (used to skip proxying built assets)
    private static readonly Regex AssetHashRegex = new Regex(
        $@"{Regex.Escape(ViteAssetsBasePath)}{AssetHashPattern}{SupportedAssetExtensions}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Cached compiled regexes - initialized once at startup
    private static Regex? _vueAppRouteRegex;
    private static Regex? _vueAppAssetRegex;

    /// <summary>
    /// Initializes compiled regexes for the Vue app patterns
    /// </summary>
    private static void InitializeRegexes(string[] vueAppNames)
    {
        if (_vueAppRouteRegex == null)
        {
            var vueAppsPattern = string.Join("|", vueAppNames);
            _vueAppRouteRegex = new Regex(string.Format(VueAppRoutePattern, vueAppsPattern),
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _vueAppAssetRegex = new Regex(string.Format(VueAppAssetPattern, vueAppsPattern),
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Determines if a request should be proxied to the Vite development server
    /// </summary>
    public static bool ShouldProxyToVite(HttpContext context, string[] vueAppNames)
    {
        var path = context.Request.Path;

        // Skip proxying built asset files with hashes - serve as static files
        if (path.HasValue && AssetHashRegex.IsMatch(path.Value))
        {
            return false;
        }

        // Proxy if path starts with /vue or /2/vue (Vite base paths)
        if (path.StartsWithSegments("/vue") || path.StartsWithSegments("/2/vue"))
            return true;

        // Proxy if path is a Vue app route (e.g., /CTS, /Computing) or Vue app asset file
        if (path.HasValue)
        {
            // Initialize compiled regexes on first use
            InitializeRegexes(vueAppNames);

            var pathValue = path.Value;

            // Match Vue app routes: /CTS, /Computing, /Students (exact match or with sub-paths)
            if (_vueAppRouteRegex!.IsMatch(pathValue))
            {
                return true;
            }

            // Match: /AppName/file.ext (assets in app subdirectories)
            // OR: /appname.ext (root-level entry files like /cts.ts)
            return _vueAppAssetRegex!.IsMatch(pathValue);
        }

        return false;
    }

    /// <summary>
    /// Builds the target URL for proxying to Vite development server
    /// </summary>
    public static string BuildViteUrl(PathString path, QueryString queryString, string[] vueAppNames)
    {
        var pathValue = path.Value;
        // Get Vite server URL from environment variable or use default
        var viteBase = Environment.GetEnvironmentVariable("VITE_SERVER_URL") ?? "https://localhost:5173";

        // Pass HTML files to Vite with full path (matches base: '/2/vue/')
        if (path.StartsWithSegments("/2/vue") && pathValue.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            return viteBase + pathValue + queryString;
        }

        // Already has /vue or /2/vue prefix - pass through as-is
        if (path.StartsWithSegments("/vue") || path.StartsWithSegments("/2/vue"))
        {
            return viteBase + pathValue + queryString;
        }

        // Check if it's a root-level entry file (e.g., /cts.ts)
        var fileName = Path.GetFileName(pathValue);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(pathValue);

        foreach (var appName in vueAppNames)
        {
            // Vue app route: /CTS -> /2/vue/src/CTS/index.html (for Vite dev server)
            if (string.Equals(pathValue, $"/{appName}", StringComparison.OrdinalIgnoreCase) ||
                pathValue?.StartsWith($"/{appName}/", StringComparison.OrdinalIgnoreCase) == true)
            {
                return $"{viteBase}/2/vue/src/{appName}/index.html{queryString}";
            }
            else if (string.Equals(fileNameWithoutExt, appName, StringComparison.OrdinalIgnoreCase))
            {
                // Root-level entry file: /cts.ts -> /2/vue/src/CTS/cts.ts
                return $"{viteBase}/2/vue/src/{appName}/{fileName}{queryString}";
            }
        }

        // Fallback - shouldn't normally reach here
        return $"{viteBase}/2/vue/src{pathValue}{queryString}";
    }

    /// <summary>
    /// Creates an HTTP request message for proxying to Vite server
    /// </summary>
    public static HttpRequestMessage CreateProxyRequest(HttpContext context, string targetUrl)
    {
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

        // Only copy request body for methods that typically support it
        var method = context.Request.Method.ToUpperInvariant();
        if (method != "GET" && method != "HEAD")
        {
            if ((context.Request.ContentLength.HasValue && context.Request.ContentLength > 0) ||
                (!context.Request.ContentLength.HasValue && context.Request.Body.CanRead))
            {
                requestMessage.Content = new StreamContent(context.Request.Body);

                // Copy content-specific headers to Content.Headers after creating StreamContent
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                    {
                        requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }
        }

        // Copy non-content request headers, excluding problematic ones
        foreach (var header in context.Request.Headers)
        {
            // Skip HTTP/2 pseudo-headers, connection-specific headers, and content headers (already handled above)
            if (!header.Key.StartsWith(":") &&
                !header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(header.Key, "host", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(header.Key, "connection", StringComparison.OrdinalIgnoreCase))
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        return requestMessage;
    }

    /// <summary>
    /// Copies the proxy response back to the client
    /// </summary>
    public static async Task CopyProxyResponse(HttpContext context, HttpResponseMessage response)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = (int)response.StatusCode;

            // Headers that shouldn't be copied from the upstream response
            var headersToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "connection", "transfer-encoding", "keep-alive", "upgrade",
                "proxy-connection", "server", "date", "content-length"
            };

            // Copy response headers
            foreach (var header in response.Headers.Concat(response.Content.Headers))
            {
                if (!headersToSkip.Contains(header.Key))
                {
                    try
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                    catch (Exception headerEx)
                    {
                        // Use structured logging for header errors
                        context.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("ViteProxyHelpers")
                            .LogWarning(headerEx, "Failed to set header '{HeaderKey}' in proxy response.", header.Key);
                    }
                }
            }
        }

        await response.Content.CopyToAsync(context.Response.Body);
    }

    /// <summary>
    /// Handles proxy errors with fallback to static files
    /// </summary>
    public static async Task HandleProxyError(HttpContext context, Exception ex, ILogger logger, string[] vueAppNames)
    {
        // Log the proxy failure with structured logging
        var targetUrl = BuildViteUrl(context.Request.Path, context.Request.QueryString, vueAppNames);
        var safeMethod = context.Request.Method.Replace("\r", "").Replace("\n", "");
        logger.LogWarning(ex, "Vite proxy failed for {Method} {RequestPath} -> {TargetUrl}",
            safeMethod,
            context.Request.Path + context.Request.QueryString,
            targetUrl);

        if (!context.Response.HasStarted)
        {
            // If Vite server is not running, try to serve static files as fallback
            try
            {
                // Determine the static file path
                string? staticPath = null;
                if (context.Request.Path.StartsWithSegments("/vue") || context.Request.Path.StartsWithSegments("/2/vue"))
                {
                    staticPath = context.Request.Path.Value?.Replace("/2/vue", "/vue") ?? "/vue";
                }
                else
                {
                    // Check if it's a Vue app route that should map to index.html
                    var pathValue = context.Request.Path.Value;
                    foreach (var appName in vueAppNames)
                    {
                        if (string.Equals(pathValue, $"/{appName}", StringComparison.OrdinalIgnoreCase) ||
                            pathValue?.StartsWith($"/{appName}/", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            staticPath = $"/vue/src/{appName}/index.html";
                            break;
                        }
                    }

                    // If no app match, map as asset file
                    if (staticPath == null)
                    {
                        staticPath = "/vue/src" + context.Request.Path;
                    }
                }

                var physicalPath = Path.Combine(context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath,
                    staticPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(physicalPath))
                {
                    var contentType = GetContentType(Path.GetExtension(physicalPath));
                    context.Response.ContentType = contentType;
                    await context.Response.SendFileAsync(physicalPath);
                    return;
                }
            }
            catch (Exception fileEx)
            {
                var safePath = context.Request.Path.ToString().Replace("\r", "").Replace("\n", "");
                logger.LogWarning(fileEx, "Failed to serve static file fallback for {Path}", safePath);
            }

            context.Response.StatusCode = 502;
            await context.Response.WriteAsync("Vite dev server not running. Please start the frontend development server or build static files as appropriate.");
        }
    }

    /// <summary>
    /// Gets the appropriate content type for a file extension using ASP.NET
    /// Core's FileExtensionContentTypeProvider
    /// </summary>
    public static string GetContentType(string extension)
    {
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        if (provider.TryGetContentType("file" + extension, out var contentType))
        {
            return contentType;
        }
        return "application/octet-stream";
    }
}