using Microsoft.Extensions.Caching.Memory;
using Viper.Models.AAUD;

namespace Viper.test.Classes
{
    [Collection(HttpHelperCacheCollection.Name)]
    public sealed class UserHelperCacheTests : IDisposable
    {
        private MemoryCache? _installed;

        // Empty, not dispose: HttpHelper.Cache is a process-wide static, so a disposed instance would
        // make every later test throw.
        public void Dispose()
        {
            _installed?.Clear();
        }

        private const string MothraId = "00012345";

        // Spelled out rather than shared with UserHelper so a rename there has to be deliberate.
        private static readonly string[] CacheKeys =
        {
            "Roles-" + MothraId,
            "PermissionsAssigned-" + MothraId + "-True",
            "PermissionsAssigned-" + MothraId + "-False",
            "PermissionsInherited-" + MothraId + "-True",
            "PermissionsInherited-" + MothraId + "-False",
        };

        private IMemoryCache ConfigureCache()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            HttpHelper.Configure(memoryCache, null!, null!, null!, null!, null!);
            _installed = memoryCache;
            return memoryCache;
        }

        [Fact]
        public void ClearCachedRolesAndPermissions_RemovesEveryRoleAndPermissionKey()
        {
            var memoryCache = ConfigureCache();
            foreach (string key in CacheKeys)
            {
                memoryCache.Set(key, "cached");
            }

            new UserHelper().ClearCachedRolesAndPermissions(new AaudUser { MothraId = MothraId });

            foreach (string key in CacheKeys)
            {
                Assert.False(memoryCache.TryGetValue(key, out _), $"{key} was left in the cache");
            }
        }

        [Fact]
        public void ClearCachedRolesAndPermissions_LeavesOtherUsersEntriesAlone()
        {
            var memoryCache = ConfigureCache();
            memoryCache.Set("Roles-99999999", "cached");

            new UserHelper().ClearCachedRolesAndPermissions(new AaudUser { MothraId = MothraId });

            Assert.True(memoryCache.TryGetValue("Roles-99999999", out _));
        }

        [Fact]
        public void ClearCachedRolesAndPermissions_ByMothraId_NeedsNoAaudUser()
        {
            var memoryCache = ConfigureCache();
            foreach (string key in CacheKeys)
            {
                memoryCache.Set(key, "cached");
            }

            // The interceptor only has the RAPS-side id, so this overload is the one it calls.
            UserHelper.ClearCachedRolesAndPermissions(MothraId);

            foreach (string key in CacheKeys)
            {
                Assert.False(memoryCache.TryGetValue(key, out _), $"{key} was left in the cache");
            }
        }

        [Fact]
        public void ClearCachedRolesAndPermissions_IgnoresAnEmptyMothraId()
        {
            var memoryCache = ConfigureCache();
            memoryCache.Set("Roles-", "cached");

            UserHelper.ClearCachedRolesAndPermissions(string.Empty);

            // Guards the old LoginId behaviour, where users without one shared the "Roles-" key.
            Assert.True(memoryCache.TryGetValue("Roles-", out _));
        }
    }
}
