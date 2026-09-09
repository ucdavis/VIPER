using Microsoft.Extensions.Caching.Memory;

namespace Web.Authorization
{
    /// <summary>
    /// Remembers Entra sessions that were signed out upstream, so the sign-in cookies they issued
    /// stop being accepted.
    /// </summary>
    /// <remarks>
    /// The sign-in cookie is self-contained: it carries the principal and is trusted on its
    /// signature alone, so ordinarily nothing on the server can invalidate one before it expires.
    /// Front-channel logout needs exactly that, and it cannot simply clear the cookie: Entra loads
    /// the logout URL in a hidden iframe on its own origin, a cross-site context where a
    /// SameSite=Lax cookie is neither sent nor accepted. A revocation list consulted on every
    /// request is what closes the gap.
    /// </remarks>
    public class EntraSessionRevocationStore
    {
        // Namespaced because IMemoryCache is shared with roles, permissions and photos.
        private const string KeyPrefix = "entra-revoked-sid:";

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _retention;

        /// <param name="cache">The app-wide memory cache.</param>
        /// <param name="retention">
        /// How long a revocation is remembered. Must be at least the sign-in cookie lifetime:
        /// once the cookie can no longer authenticate on its own there is nothing left to revoke,
        /// and holding the entry longer only wastes memory.
        /// </param>
        public EntraSessionRevocationStore(IMemoryCache cache, TimeSpan retention)
        {
            _cache = cache;
            _retention = retention;
        }

        /// <summary>Marks an Entra session id as signed out. Ignores null or blank ids.</summary>
        // ponytail: in-process, so revocations are lost on an app restart and are not shared
        // between nodes. That is sound only because VIPER 2 runs one instance per environment
        // (AddDataProtection() keeps its key ring locally, so a second node could not read the
        // first's cookies anyway). Move this to IDistributedCache if either fact changes.
        public void Revoke(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            _cache.Set(KeyPrefix + sessionId, true, _retention);
        }

        /// <summary>True when this Entra session id has been signed out upstream.</summary>
        public bool IsRevoked(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            return _cache.TryGetValue(KeyPrefix + sessionId, out _);
        }
    }
}
