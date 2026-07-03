using System.Globalization;
using System.Threading.RateLimiting;
using Viper.Classes.Utilities;

namespace Viper.Areas.CMS.Services
{
    /// <summary>
    /// Rate limiting for the CMS download endpoint (/CMS/Files). ZIP requests assemble
    /// multi-file archives in memory, so they get a strict token bucket; single-file
    /// downloads get a generous sliding window that allows image-heavy pages but stops
    /// sustained scraping. Buckets are per login id when authenticated, per client IP
    /// otherwise (forwarded headers are already resolved for the Cloudflare/F5 chain,
    /// so RemoteIpAddress is the real client on TEST/PROD).
    /// </summary>
    public static class CmsDownloadRateLimiting
    {
        public const string PolicyName = "CmsDownloads";

        private const string ZipPrefix = "zip|";
        private const string FilePrefix = "file|";

        // Defaults; override under CMS:DownloadRateLimit in appsettings.
        private const int DefaultFilePermitsPerMinute = 100;
        private const int DefaultZipTokenLimit = 5;
        private const int DefaultZipTokensPerMinute = 1;

        public static IServiceCollection AddCmsDownloadRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            int filePermits = configuration.GetValue("CMS:DownloadRateLimit:FilePermitsPerMinute", DefaultFilePermitsPerMinute);
            int zipTokenLimit = configuration.GetValue("CMS:DownloadRateLimit:ZipTokenLimit", DefaultZipTokenLimit);
            int zipTokensPerMinute = configuration.GetValue("CMS:DownloadRateLimit:ZipTokensPerMinute", DefaultZipTokensPerMinute);

            // Misconfigured (zero/negative) values would make the limiter option
            // constructors throw on the first request, so fall back to defaults.
            if (filePermits <= 0) filePermits = DefaultFilePermitsPerMinute;
            if (zipTokenLimit <= 0) zipTokenLimit = DefaultZipTokenLimit;
            if (zipTokensPerMinute <= 0) zipTokensPerMinute = DefaultZipTokensPerMinute;

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, ct) =>
                {
                    var httpContext = context.HttpContext;

                    // Visibility for tuning the limits on TEST/PROD. Sanitize the partition key
                    // too: for anonymous requests it falls back to the proxy-resolved client IP,
                    // which can derive from a user-controllable forwarded header.
                    httpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger(nameof(CmsDownloadRateLimiting))
                        .LogInformation("Download rate limit hit for {PartitionKey} on {Path}",
                            LogSanitizer.SanitizeString(GetPartitionKey(httpContext)),
                            LogSanitizer.SanitizeString(httpContext.Request.Path));

                    // A response already in flight can't take headers or a new body.
                    if (httpContext.Response.HasStarted)
                    {
                        return;
                    }

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        httpContext.Response.Headers.RetryAfter =
                            ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                    }
                    httpContext.Response.ContentType = "text/plain";
                    await httpContext.Response.WriteAsync(
                        "Too many download requests. Please wait a moment and try again.", ct);
                };

                options.AddPolicy(PolicyName, httpContext =>
                {
                    var key = GetPartitionKey(httpContext);
                    return key.StartsWith(ZipPrefix, StringComparison.Ordinal)
                        ? RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = zipTokenLimit,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            TokensPerPeriod = zipTokensPerMinute,
                            AutoReplenishment = true,
                            QueueLimit = 0
                        })
                        : RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = filePermits,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        });
                });
            });

            return services;
        }

        /// <summary>
        /// Partition key: request type (zip vs single file, matching CMSController.Files'
        /// dispatch on the ids parameter) plus the caller's login id, or client IP for
        /// anonymous/public requests.
        /// </summary>
        public static string GetPartitionKey(HttpContext httpContext)
        {
            string prefix = IsZipRequest(httpContext) ? ZipPrefix : FilePrefix;
            string? login = httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.Identity.Name
                : null;
            string caller = !string.IsNullOrEmpty(login)
                ? login
                : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return prefix + caller;
        }

        public static bool IsZipRequest(HttpContext httpContext)
        {
            return !string.IsNullOrEmpty(httpContext.Request.Query["ids"]);
        }
    }
}
