namespace Viper.Classes
{
    /// <summary>
    /// Cloudflare's published IPv4/IPv6 networks, used to mark CF as a known
    /// proxy in ForwardedHeadersOptions. Fetched from cloudflare.com at startup
    /// so we automatically pick up rotations; falls back to a hardcoded snapshot
    /// when the fetch fails (CF outage during deploy, sandboxed network, etc).
    /// </summary>
    public static class CloudflareNetworks
    {
        // Snapshot of https://www.cloudflare.com/ips/ - only used when the
        // runtime fetch fails. Refresh occasionally if logs show this falling
        // through and current CF IPs aren't in the list.
        private static readonly string[] HardcodedFallback =
        [
            "173.245.48.0/20",
            "103.21.244.0/22",
            "103.22.200.0/22",
            "103.31.4.0/22",
            "141.101.64.0/18",
            "108.162.192.0/18",
            "190.93.240.0/20",
            "188.114.96.0/20",
            "197.234.240.0/22",
            "198.41.128.0/17",
            "162.158.0.0/15",
            "104.16.0.0/13",
            "104.24.0.0/14",
            "172.64.0.0/13",
            "131.0.72.0/22",
            "2400:cb00::/32",
            "2606:4700::/32",
            "2803:f800::/32",
            "2405:b500::/32",
            "2405:8100::/32",
            "2a06:98c0::/29",
            "2c0f:f248::/32",
        ];

        public static IReadOnlyList<string> FetchOrFallback(NLog.Logger logger)
        {
            try
            {
                // Startup-only call, not in a hot path - the socket-exhaustion concern
                // behind ShortLivedHttpClient doesn't apply here.
                // ReSharper disable once ShortLivedHttpClient
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(5);
                var v4 = http.GetStringAsync("https://www.cloudflare.com/ips-v4/").GetAwaiter().GetResult();
                var v6 = http.GetStringAsync("https://www.cloudflare.com/ips-v6/").GetAwaiter().GetResult();
                var cidrs = (v4 + "\n" + v6)
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                logger.Info("Fetched {Count} Cloudflare networks from cloudflare.com", cidrs.Length);
                return cidrs;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.Warn(ex, "Failed to fetch Cloudflare IP ranges; using hardcoded fallback ({Count} entries)", HardcodedFallback.Length);
                return HardcodedFallback;
            }
        }
    }
}
