using System.Globalization;
using Viper.Classes.Utilities;
using Viper.Models.VIPER;

namespace Viper.test.Classes
{
    public class SessionTimeoutServiceTests
    {
        [Fact]
        public void ToStatus_StoredRow_ReportsThatExpiryWithLocalOffset()
        {
            DateTime expiry = DateTime.Now.AddMinutes(10);

            var status = SessionTimeoutService.ToStatus(new SessionTimeout { SessionTimeoutDateTime = expiry }, authenticated: true);

            Assert.InRange(status.SecondsUntilTimeout, 590, 600);
            var parsed = DateTimeOffset.ParseExact(status.SessionTimeoutDateTime, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            Assert.Equal(expiry.ToString("s"), parsed.DateTime.ToString("s"));
            Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(expiry), parsed.Offset);
        }

        [Fact]
        public void ToStatus_AuthenticatedWithoutRow_GrantsFullWindow()
        {
            var status = SessionTimeoutService.ToStatus(null, authenticated: true);

            Assert.Equal(SessionTimeoutService.SessionTimeoutSeconds, status.SecondsUntilTimeout);
        }

        [Fact]
        public void ToStatus_Anonymous_ReportsExpired()
        {
            Assert.Equal(0, SessionTimeoutService.ToStatus(null, authenticated: false).SecondsUntilTimeout);
        }
    }
}
