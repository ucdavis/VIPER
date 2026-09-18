using Microsoft.Extensions.Caching.Memory;
using Web.Authorization;

namespace Test.Classes
{
    // This store is the only thing standing between "signed out of Entra" and "still signed in to
    // VIPER", because the sign-in cookie cannot otherwise be invalidated before it expires. A false
    // negative here leaves a session alive for up to 12 hours after the user signed out.
    public class EntraSessionRevocationStoreTests
    {
        private static EntraSessionRevocationStore Store() =>
            new(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromHours(12));

        [Fact]
        public void IsRevoked_AfterRevoke_ReturnsTrue()
        {
            var store = Store();

            store.Revoke("session-a");

            Assert.True(store.IsRevoked("session-a"));
        }

        [Fact]
        public void IsRevoked_SessionNeverRevoked_ReturnsFalse()
        {
            Assert.False(Store().IsRevoked("session-a"));
        }

        [Fact]
        public void Revoke_OneSession_LeavesOtherSessionsAlone()
        {
            var store = Store();

            store.Revoke("session-a");

            Assert.False(store.IsRevoked("session-b"));
        }

        // A CAS login has no sid, so every request it makes asks about a blank one. Answering true
        // would sign out every CAS user in the app.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsRevoked_BlankSessionId_ReturnsFalse(string? sessionId)
        {
            var store = Store();
            store.Revoke("session-a");

            Assert.False(store.IsRevoked(sessionId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Revoke_BlankSessionId_IsIgnored(string? sessionId)
        {
            var store = Store();

            store.Revoke(sessionId);

            Assert.False(store.IsRevoked(sessionId));
        }

        [Fact]
        public void IsRevoked_IsCaseSensitive()
        {
            var store = Store();

            store.Revoke("Session-A");

            // Entra session ids are opaque, so nothing licenses folding case: treating two
            // different ids as one would revoke a session that was never signed out.
            Assert.False(store.IsRevoked("session-a"));
        }
    }
}
