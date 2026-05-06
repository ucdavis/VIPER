using Microsoft.AspNetCore.HttpOverrides;
using NLog;
using System.Net;
using Viper.Classes.Utilities;

namespace Viper.Classes
{
    /// <summary>
    /// Wires <see cref="ForwardedHeadersOptions"/> for the test/prod proxy
    /// chain (User -> Cloudflare -> F5 -> app). Kept out of Program.cs so
    /// Main stays small.
    /// </summary>
    public static class ForwardedHeadersExtensions
    {
        // The F5's internal IP. Static so it's only parsed once per process.
        private static readonly IPAddress F5InternalIp = IPAddress.Parse("192.168.56.134");

        /// <summary>
        /// Registers ForwardedHeadersOptions with the F5 + Cloudflare CIDRs as
        /// trusted proxies. No-op in Development (UseForwardedHeaders isn't
        /// applied there either, and we want to avoid the cloudflare.com fetch
        /// on every local startup).
        /// </summary>
        public static IServiceCollection AddViperForwardedHeaders(
            this IServiceCollection services,
            IHostEnvironment environment,
            Logger logger)
        {
            if (environment.IsDevelopment())
            {
                return services;
            }

            var cloudflareCidrs = CloudflareNetworks.FetchOrFallback(logger);
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownProxies.Add(F5InternalIp);

                // Cloudflare fronts vetmed.ucdavis.edu. The chain is
                // User -> Cloudflare -> F5 -> app, so the middleware must walk two
                // proxy hops to land on the real client IP. Default ForwardLimit
                // is 1, which stops at the CF edge - bump to 2.
                options.ForwardLimit = 2;
                AddCloudflareCidrs(options, cloudflareCidrs, logger);
            });

            return services;
        }

        // cidrs come from cloudflare.com (or our hardcoded fallback). A single
        // malformed entry in the live response shouldn't crash startup - skip
        // it and keep the rest of the allowlist.
        private static void AddCloudflareCidrs(
            ForwardedHeadersOptions options,
            IEnumerable<string> cidrs,
            Logger logger)
        {
            foreach (var cidr in cidrs)
            {
                try
                {
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
                }
                catch (FormatException ex)
                {
                    logger.Warn(ex, "Skipping invalid Cloudflare CIDR: {Cidr}", LogSanitizer.SanitizeString(cidr));
                }
            }
        }
    }
}
