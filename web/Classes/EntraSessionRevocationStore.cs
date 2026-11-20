using Microsoft.Extensions.Caching.Distributed;
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
    /// <para>
    /// The list lives in the distributed cache, which both blue/green slots share, so a
    /// revocation survives a slot flip or a restart and reaches a slot that has not served the
    /// session yet. The memory cache in front of it only spares the steady state a round trip.
    /// </para>
    /// </remarks>
    public class EntraSessionRevocationStore
    {
        // Namespaced because both caches are shared with other features.
        private const string KeyPrefix = "entra-revoked-sid:";
        private const string ActiveKeyPrefix = "entra-active-sid:";

        // How long this process trusts its own copy before asking the shared store again. It is
        // also the longest a revocation recorded by the other slot can go unnoticed here.
        private static readonly TimeSpan LocalTtl = TimeSpan.FromMinutes(1);

        private static readonly byte[] Marker = [1];

        private readonly IMemoryCache _local;
        private readonly IDistributedCache _shared;
        private readonly TimeSpan _retention;

        /// <summary>Creates a store over the shared cache, fronted by the app-wide memory cache.</summary>
        /// <param name="local">The app-wide memory cache.</param>
        /// <param name="shared">The cache every instance of the app reads and writes.</param>
        /// <param name="retention">
        /// How long a revocation is remembered. Must be at least the sign-in cookie lifetime:
        /// once the cookie can no longer authenticate on its own there is nothing left to revoke,
        /// and holding the entry longer only wastes space.
        /// </param>
        public EntraSessionRevocationStore(IMemoryCache local, IDistributedCache shared, TimeSpan retention)
        {
            _local = local;
            _shared = shared;
            _retention = retention;
        }

        /// <summary>
        /// Records that a cookie carrying this session id is in use, which is what makes the id
        /// revocable. Called on every authenticated request.
        /// </summary>
        public void NoteActive(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            var key = ActiveKeyPrefix + sessionId;
            if (_local.TryGetValue(key, out _))
            {
                return;
            }

            // Outlives the local copy, so the shared marker never lapses while the cookie is in use.
            _shared.Set(key, Marker, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _retention + LocalTtl
            });
            _local.Set(key, true, LocalTtl);
        }

        /// <summary>Marks an Entra session id as signed out. Ignores blank and unknown ids.</summary>
        /// <remarks>
        /// The caller is an anonymous endpoint anyone can reach. A length cap alone would still let
        /// a stranger write one lasting entry per distinct id into a cache shared with roles,
        /// permissions and photos, so only ids a live cookie has presented are admitted.
        /// </remarks>
        public void Revoke(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            // Asks the shared store, not the local copy: the logout can land on a slot that has
            // never served this session.
            if (_shared.Get(ActiveKeyPrefix + sessionId) is null)
            {
                return;
            }

            var key = KeyPrefix + sessionId;
            _shared.Set(key, Marker, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _retention
            });
            _local.Set(key, true, _retention);
        }

        /// <summary>True when this Entra session id has been signed out upstream.</summary>
        public bool IsRevoked(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            var key = KeyPrefix + sessionId;
            if (_local.TryGetValue(key, out bool revoked))
            {
                return revoked;
            }

            // A revocation is final, so it is kept for the full retention. A "not revoked" answer
            // is only trusted briefly, in case the other slot records one.
            revoked = _shared.Get(key) is not null;
            _local.Set(key, revoked, revoked ? _retention : LocalTtl);
            return revoked;
        }
    }
}
