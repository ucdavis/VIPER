using Microsoft.Extensions.Caching.Memory;

// Joins the existing Web.Authorization cluster in this folder (CasSettings,
// ClaimsTransformer, PermissionAttribute) rather than the folder-derived Viper.Classes.
// ReSharper disable once CheckNamespace
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
        private const string ActiveKeyPrefix = "entra-active-sid:";

        private readonly IMemoryCache _cache;
        private readonly TimeSpan _retention;

        /// <summary>Creates a store over the app-wide memory cache.</summary>
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

        /// <summary>
        /// Records that a cookie carrying this session id is in use, which is what makes the id
        /// revocable. Called on every authenticated request, so a restart rebuilds the markers.
        /// </summary>
        public void NoteActive(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            // Sliding, so reading the marker refreshes it and the steady state is a lookup.
            var key = ActiveKeyPrefix + sessionId;
            if (!_cache.TryGetValue(key, out _))
            {
                _cache.Set(key, true, new MemoryCacheEntryOptions { SlidingExpiration = _retention });
            }
        }

        /// <summary>Marks an Entra session id as signed out. Ignores blank and unknown ids.</summary>
        /// <remarks>
        /// The caller is an anonymous endpoint anyone can reach. A length cap alone would still let
        /// a stranger write one lasting entry per distinct id into a cache shared with roles,
        /// permissions and photos, so only ids a live cookie has presented are admitted.
        /// </remarks>
        // ponytail: in-process, so a restart drops both the revocations and the active markers that
        // gate them, leaving surviving cookies unrevocable until each makes its next request. That
        // is sound only because VIPER 2 runs one instance per environment
        // (AddDataProtection() keeps its key ring locally, so a second node could not read the
        // first's cookies anyway). Move this to IDistributedCache if either fact changes.
        public void Revoke(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            if (!_cache.TryGetValue(ActiveKeyPrefix + sessionId, out _))
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
