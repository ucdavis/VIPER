using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.test.Students;

namespace Viper.test.Classes
{
    [Collection(HttpHelperCacheCollection.Name)]
    public sealed class UserHelperGetByLoginIdTests : IDisposable
    {
        private const string LoginId = "jdoe";

        private readonly FakeClock _clock = new();
        private readonly MemoryCache _memoryCache;
        private readonly TestableAAUDContext _context;

        public UserHelperGetByLoginIdTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions { Clock = _clock });
            HttpHelper.Configure(_memoryCache, null!, null!, null!, null!, null!, null!);

            var options = new DbContextOptionsBuilder<AAUDContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TestableAAUDContext(options);
        }

        // Clear, not dispose: HttpHelper.Cache is a process-wide static, so a disposed instance would
        // make every later test throw.
        public void Dispose()
        {
            _memoryCache.Clear();
            _context.Dispose();
        }

        private void AddUser(int current = 1, int future = 0)
        {
            _context.AaudUsers.Add(new AaudUser
            {
                AaudUserId = 1,
                ClientId = "C1",
                MothraId = "00012345",
                LoginId = LoginId,
                FirstName = "Jane",
                LastName = "Doe",
                DisplayFirstName = "Jane",
                DisplayLastName = "Doe",
                DisplayFullName = "Jane Doe",
                Current = current,
                Future = future,
            });
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
        }

        private void RemoveUsers()
        {
            _context.AaudUsers.RemoveRange(_context.AaudUsers);
            _context.SaveChanges();
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        public void GetByLoginId_ReturnsCurrentOrFutureUser(int current, int future)
        {
            AddUser(current, future);

            var result = new UserHelper().GetByLoginId(_context, LoginId);

            Assert.NotNull(result);
            Assert.Equal(LoginId, result.LoginId);
        }

        [Fact]
        public void GetByLoginId_ReturnsNullForInactiveUser()
        {
            AddUser(current: 0, future: 0);

            Assert.Null(new UserHelper().GetByLoginId(_context, LoginId));
        }

        [Fact]
        public void GetByLoginId_ServesFoundUserFromCache()
        {
            AddUser();
            var helper = new UserHelper();
            helper.GetByLoginId(_context, LoginId);

            RemoveUsers();

            Assert.NotNull(helper.GetByLoginId(_context, LoginId));
        }

        [Fact]
        public void GetByLoginId_DoesNotCacheMiss()
        {
            var helper = new UserHelper();
            Assert.Null(helper.GetByLoginId(_context, LoginId));

            // A user who becomes active after a failed lookup must not be locked out by a cached null.
            AddUser();

            Assert.NotNull(helper.GetByLoginId(_context, LoginId));
        }

        [Fact]
        public void GetByLoginId_CachedUserExpires()
        {
            AddUser();
            var helper = new UserHelper();
            helper.GetByLoginId(_context, LoginId);

            // A deactivated user must drop out once the cache entry expires.
            RemoveUsers();
            AddUser(current: 0, future: 0);
            _clock.UtcNow = _clock.UtcNow.AddMinutes(11);

            Assert.Null(helper.GetByLoginId(_context, LoginId));
        }

        [Fact]
        public void GetByLoginId_ReturnsNullForNullLoginId()
        {
            Assert.Null(new UserHelper().GetByLoginId(_context, null));
        }

        private sealed class FakeClock : ISystemClock
        {
            public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
        }
    }
}
