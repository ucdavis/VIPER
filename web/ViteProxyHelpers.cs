using System.Net;
using System.Text.RegularExpressions;

namespace Web;

/// <summary>
/// Helper methods for proxying requests to the Vite development server.
/// </summary>
/// <remarks>
/// <para>
/// <b>Security considerations:</b>
/// <list type="bullet">
///   <item>
///     <description>
///       Path validation is performed using regular expressions to ensure that only requests for valid Vue app routes and assets are proxied to the Vite development server.
///       Requests for built asset files with Vite hashes (e.g., <c>main-abc123.js</c>) are excluded from proxying and are instead served as static files.
///     </description>
///   </item>
///   <item>
///     <description>
///       The helper methods are designed to prevent proxying of arbitrary or potentially malicious paths by strictly matching allowed patterns.
///     </description>
///   </item>
///   <item>
///     <description>
///       Proxy error handling should be implemented by the caller to ensure that errors from the Vite server do not expose sensitive information to clients.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
internal static partial class ViteProxyHelpers
{
    // Base path for Vite assets - make configurable for maintainability
    private const string ViteAssetsBasePath = "/2/vue/assets/";

    // Regex patterns for better maintainability and testability
    private const string AssetHashPattern = @".*-[A-Za-z0-9_-]{6,}\.";
    private const string SupportedAssetExtensions = @"(js|css|png|jpg|jpeg|gif|svg|ico|woff|woff2|ttf|eot)";
    private const string VueAppRoutePattern = @"^/({0})(/.*)?$";
    private const string VueAppAssetPattern = @"^/({0})/.*\.(js|ts|css|map|vue|json)$|^/({0})\.(js|ts|css|map|vue|json)$";

    // Regex for built asset files with Vite hashes (used to skip proxying built assets)
    private static readonly Regex AssetHashRegex = new Regex(
        $@"{Regex.Escape(ViteAssetsBasePath)}{AssetHashPattern}{SupportedAssetExtensions}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Cached compiled regexes - initialized once at startup
    private static Regex? _vueAppRouteRegex;
    private static Regex? _vueAppAssetRegex;
    // Lock for safe one-time initialization
    private static readonly object _regexInitLock = new();

    /// <summary>
    /// Initializes compiled regexes for the Vue app patterns
    /// </summary>
    private static void InitializeRegexes(string[] vueAppNames)
    {
        if (_vueAppRouteRegex != null) return;
        lock (_regexInitLock)
        {
            if (_vueAppRouteRegex == null)
            {
                // Escape app names to avoid regex injection and build a safe alternation list.
                var escaped = vueAppNames.Select(Regex.Escape);
                var vueAppsPattern = string.Join("|", escaped);

                // If no app names, create regexes that never match
                if (string.IsNullOrEmpty(vueAppsPattern))
                {
                    // @"(?!)" is a negative lookahead that never matches any input; used here to ensure no routes/assets match when no app names are provided.
                    _vueAppRouteRegex = new Regex(@"(?!)", RegexOptions.Compiled);
                    _vueAppAssetRegex = new Regex(@"(?!)", RegexOptions.Compiled);
                }
                else
                {
                    _vueAppRouteRegex = new Regex(string.Format(VueAppRoutePattern, vueAppsPattern),
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    _vueAppAssetRegex = new Regex(string.Format(VueAppAssetPattern, vueAppsPattern),
                        RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }
            }
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

        // If no Vue app names are provided, don't proxy app-specific routes
        if (vueAppNames == null || vueAppNames.Length == 0)
        {
            return false;
        }

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
        // Validate viteBase to avoid misconfiguration / SSRF risk from malformed or unexpected values.
        // Reject URIs containing userinfo (credentials) and require http/https.
        if (!Uri.TryCreate(viteBase, UriKind.Absolute, out var viteBaseUri) ||
            (viteBaseUri.Scheme != Uri.UriSchemeHttp && viteBaseUri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(viteBaseUri.UserInfo))
        {
            // fall back to safe default
            viteBaseUri = new Uri("https://localhost:5173");
        }

        // Uri.TryCreate with Absolute can match non-HTTP schemes (e.g. file://) — only reject http(s).
        var safePath = pathValue ?? string.Empty;
        if (Uri.TryCreate(safePath, UriKind.Absolute, out var parsedUri) &&
            (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps))
        {
            // If an absolute HTTP(S) URI was provided, treat as not allowed and fallback
            safePath = "/";
        }

        // Use the validated Uri to combine base + relative path safely to avoid malformed URLs / SSRF
        var baseUri = viteBaseUri;
        if (safePath.StartsWith("/2/vue") && safePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var combinedUri = new Uri(baseUri, safePath);
            return combinedUri.ToString() + queryString;
        }

        if (safePath.StartsWith("/vue") || safePath.StartsWith("/2/vue"))
        {
            var combinedUri = new Uri(baseUri, safePath);
            return combinedUri.ToString() + queryString;
        }

        // Pre-compute values to avoid repeated string operations
        var fileName = Path.GetFileName(safePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(safePath);
        var fileExtension = Path.GetExtension(safePath);
        var hasExtension = !string.IsNullOrEmpty(fileExtension);

        foreach (var appName in vueAppNames)
        {
            var appRoute = $"/{appName}";
            var appRoutePrefix = $"/{appName}/";

            // Check for root-level entry file first: /cts.ts -> /2/vue/src/CTS/cts.ts
            if (hasExtension && string.Equals(fileNameWithoutExt, appName, StringComparison.OrdinalIgnoreCase))
            {
                var combinedUri = new Uri(baseUri, $"/2/vue/src/{appName}/{fileName}");
                return combinedUri.ToString() + queryString;
            }

            // Vue app routes (exact match or sub-routes without extensions): /CTS or /CTS/dashboard -> index.html
            if (string.Equals(safePath, appRoute, StringComparison.OrdinalIgnoreCase) ||
                (safePath.StartsWith(appRoutePrefix, StringComparison.OrdinalIgnoreCase) && !hasExtension))
            {
                var combinedUri = new Uri(baseUri, $"/2/vue/src/{appName}/index.html");
                return combinedUri.ToString() + queryString;
            }
        }

        // Fallback - shouldn't normally reach here
        var fallbackUri = new Uri(baseUri, $"/2/vue/src{safePath}");
        return fallbackUri.ToString() + queryString;
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
                // Exclude headers that should be handled automatically by HttpClient
                var forbiddenContentHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Content-Length",
                    "Content-Type",
                    "Content-Encoding",
                    "Content-Disposition",
                    "Content-Range"
                };
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase) &&
                        !forbiddenContentHeaders.Contains(header.Key))
                    {
                        requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                    }
                }
            }
        }

        // Copy non-content request headers, excluding hop-by-hop and other problematic headers
        var requestHeaderSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "connection", "transfer-encoding", "keep-alive", "upgrade",
            "proxy-connection", "host", "te"
        };

        foreach (var header in context.Request.Headers)
        {
            // Skip HTTP/2 pseudo-headers, content headers (already handled above), and hop-by-hop headers
            if (!header.Key.StartsWith(':') &&
                !header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase) &&
                !requestHeaderSkip.Contains(header.Key))
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
                        var safeHeaderKey = WebUtility.HtmlEncode(header.Key);
                        context.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("ViteProxyHelpers")
                            .LogWarning(headerEx, "Failed to set header '{HeaderKey}' in proxy response.", safeHeaderKey);
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
        var safeMethod = WebUtility.HtmlEncode(context.Request.Method);
        var safeRequestPath = WebUtility.HtmlEncode((context.Request.Path + context.Request.QueryString).ToString());
        var safeTargetUrl = WebUtility.HtmlEncode(targetUrl);
        logger.LogWarning(ex, "Vite proxy failed for {Method} {RequestPath} -> {TargetUrl}",
            safeMethod,
            safeRequestPath,
            safeTargetUrl);

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

                // Prevent directory traversal: ensure the resolved physical path is within WebRootPath
                var webRoot = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootPath;
                var resolvedPhysical = Path.GetFullPath(physicalPath);
                var resolvedWebRoot = Path.GetFullPath(webRoot);

                if (!resolvedPhysical.StartsWith(resolvedWebRoot, StringComparison.OrdinalIgnoreCase))
                {
                    // Do not serve files outside the web root
                    logger.LogWarning("Attempt to access file outside web root: {Path}", resolvedPhysical);
                }
                else if (File.Exists(resolvedPhysical))
                {
                    var contentType = GetContentType(Path.GetExtension(resolvedPhysical));
                    // Set content type if not already started
                    context.Response.ContentType = contentType ?? "application/octet-stream";
                    await context.Response.SendFileAsync(resolvedPhysical);
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