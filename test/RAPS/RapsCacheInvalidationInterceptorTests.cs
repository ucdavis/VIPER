using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    /// <summary>
    /// Pins the interceptor that replaced the per-call-site invalidation, which several write paths
    /// (the nightly role refresh, the OU group sync) never called.
    /// </summary>
    [Collection(HttpHelperCacheCollection.Name)]
    public class RapsCacheInvalidationInterceptorTests : IAsyncLifetime
    {
        private const string MothraId = "00012345";

        private static readonly string[] CacheKeys =
        {
            "Roles-" + MothraId,
            "PermissionsAssigned-" + MothraId + "-True",
            "PermissionsAssigned-" + MothraId + "-False",
            "PermissionsInherited-" + MothraId + "-True",
            "PermissionsInherited-" + MothraId + "-False",
        };

        private SqliteConnection _connection = null!;
        private RAPSContext _context = null!;

        public async ValueTask InitializeAsync()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            await _connection.OpenAsync(TestContext.Current.CancellationToken);
            _context = await NewContextAsync(_connection);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static async Task<RAPSContext> NewContextAsync(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<RAPSContext>()
                .UseSqlite(connection)
                .AddInterceptors(new RapsCacheInvalidationInterceptor())
                .Options;
            var context = new RAPSContext(options);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            // Parent rows for the foreign keys the writes below depend on.
            context.TblRoles.AddRange(
                new TblRole { RoleId = 1, Role = "VIPER.Test", Application = 0, UpdateFreq = 0, AllowAllUsers = false },
                new TblRole { RoleId = 7, Role = "VIPER.Shared", Application = 0, UpdateFreq = 0, AllowAllUsers = false });
            context.TblPermissions.Add(new TblPermission { PermissionId = 1, Permission = "SVMSecure.Test" });
            // TblRoleMember.MemberId and TblMemberPermission.MemberId both FK to VwAaudUser.
            context.VwAaudUser.AddRange(
                new VwAaudUser { MothraId = MothraId, DisplayFirstName = "Test", DisplayLastName = "User", DisplayFullName = "User, Test" },
                new VwAaudUser { MothraId = "99999999", DisplayFirstName = "Other", DisplayLastName = "User", DisplayFullName = "User, Other" });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return context;
        }

        private static IMemoryCache SeedCache()
        {
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            HttpHelper.Configure(memoryCache, null!, null!, null!, null!, null!, null!);
            foreach (string key in CacheKeys)
            {
                memoryCache.Set(key, "cached");
            }
            return memoryCache;
        }

        private static void AssertCleared(IMemoryCache cache)
        {
            foreach (string key in CacheKeys)
            {
                Assert.False(cache.TryGetValue(key, out _), $"{key} was left in the cache");
            }
        }

        [Fact]
        public async Task AddingARoleMember_EvictsThatMember()
        {
            var cache = SeedCache();

            _context.TblRoleMembers.Add(new TblRoleMember { RoleId = 1, MemberId = MothraId });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            AssertCleared(cache);
        }

        [Fact]
        public async Task RemovingARoleMember_EvictsThatMember()
        {
            var member = new TblRoleMember { RoleId = 1, MemberId = MothraId };
            _context.TblRoleMembers.Add(member);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Revocation is the direction that matters: a stale allow keeps access alive.
            var cache = SeedCache();
            _context.TblRoleMembers.Remove(member);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            AssertCleared(cache);
        }

        [Fact]
        public async Task ChangingAnIndividualPermission_EvictsThatMember()
        {
            var cache = SeedCache();

            _context.TblMemberPermissions.Add(new TblMemberPermission { PermissionId = 1, MemberId = MothraId, Access = 1 });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            AssertCleared(cache);
        }

        [Fact]
        public async Task ChangingARolesPermissions_EvictsEveryMemberOfThatRole()
        {
            _context.TblRoleMembers.Add(new TblRoleMember { RoleId = 7, MemberId = MothraId });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // The write names the role, not the people, so the interceptor has to expand it.
            var cache = SeedCache();
            _context.TblRolePermissions.Add(new TblRolePermission { RoleId = 7, PermissionId = 1, Access = 1 });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            AssertCleared(cache);
        }

        [Fact]
        public async Task ChangingTheRoleItself_EvictsEveryMemberOfThatRole()
        {
            _context.TblRoleMembers.Add(new TblRoleMember { RoleId = 7, MemberId = MothraId });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // Renaming a role repoints the permissions its members inherit, so it has to expand too.
            var cache = SeedCache();
            TblRole role = await _context.TblRoles.SingleAsync(r => r.RoleId == 7, TestContext.Current.CancellationToken);
            role.Description = "Renamed";
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            AssertCleared(cache);
        }

        [Fact]
        public void SynchronousSave_EvictsToo()
        {
            var cache = SeedCache();

            // Not every RAPS write path is async, and the sync overrides stash and evict separately.
            _context.TblRoleMembers.Add(new TblRoleMember { RoleId = 1, MemberId = MothraId });
            _context.SaveChanges();

            AssertCleared(cache);
        }

        [Fact]
        public async Task AnUnrelatedWrite_LeavesTheCacheAlone()
        {
            var cache = SeedCache();

            _context.TblRoleMembers.Add(new TblRoleMember { RoleId = 1, MemberId = "99999999" });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            foreach (string key in CacheKeys)
            {
                Assert.True(cache.TryGetValue(key, out _), $"{key} should not have been evicted");
            }
        }
    }
}
