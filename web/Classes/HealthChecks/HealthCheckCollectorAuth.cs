using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Viper.Classes.HealthChecks
{
    /// <summary>
    /// Process-unique secret used by the in-app HealthChecksUI collector to bypass
    /// the InternalAllowlist IP check on its self-call to /health/detail. The token
    /// is regenerated each time the app starts; both the outbound handler and the
    /// inbound filter live in the same process, so they always agree.
    /// </summary>
    public static class HealthCheckCollectorAuth
    {
        public const string HeaderName = "X-Health-Collector-Token";

        public static string Token { get; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Constant-time comparison so we don't leak token bytes via timing.
        /// </summary>
        public static bool Matches(string? provided)
        {
            if (string.IsNullOrEmpty(provided)) return false;
            var a = Encoding.UTF8.GetBytes(provided);
            var b = Encoding.UTF8.GetBytes(Token);
            return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>
        /// True if the request carries a valid collector token header. Lets the
        /// /health/detail endpoint filter recognize the in-app self-call without
        /// touching the IP allowlist.
        /// </summary>
        public static bool IsCollectorRequest(HttpContext context)
        {
            var token = context.Request.Headers[HeaderName].FirstOrDefault();
            return Matches(token);
        }
    }
}
