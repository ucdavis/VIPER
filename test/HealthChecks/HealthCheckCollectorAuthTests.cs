using Microsoft.AspNetCore.Http;
using Viper.Classes.HealthChecks;

namespace Viper.test.HealthChecks
{
    public class HealthCheckCollectorAuthTests
    {
        [Fact]
        public void Matches_NullProvided_ReturnsFalse()
        {
            Assert.False(HealthCheckCollectorAuth.Matches(null));
        }

        [Fact]
        public void Matches_EmptyProvided_ReturnsFalse()
        {
            Assert.False(HealthCheckCollectorAuth.Matches(string.Empty));
        }

        [Fact]
        public void Matches_DifferentLength_ReturnsFalse()
        {
            Assert.False(HealthCheckCollectorAuth.Matches("short"));
        }

        [Fact]
        public void Matches_SameLengthDifferentValue_ReturnsFalse()
        {
            // Same length as the real token (32 hex chars from Guid.ToString("N")) but different bytes.
            var sameLengthFake = new string('a', HealthCheckCollectorAuth.Token.Length);
            Assert.False(HealthCheckCollectorAuth.Matches(sameLengthFake));
        }

        [Fact]
        public void Matches_RealToken_ReturnsTrue()
        {
            Assert.True(HealthCheckCollectorAuth.Matches(HealthCheckCollectorAuth.Token));
        }

        [Fact]
        public void IsCollectorRequest_HeaderAbsent_ReturnsFalse()
        {
            var ctx = new DefaultHttpContext();
            Assert.False(HealthCheckCollectorAuth.IsCollectorRequest(ctx));
        }

        [Fact]
        public void IsCollectorRequest_HeaderEmpty_ReturnsFalse()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[HealthCheckCollectorAuth.HeaderName] = string.Empty;
            Assert.False(HealthCheckCollectorAuth.IsCollectorRequest(ctx));
        }

        [Fact]
        public void IsCollectorRequest_WrongToken_ReturnsFalse()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[HealthCheckCollectorAuth.HeaderName] = "not-the-real-token";
            Assert.False(HealthCheckCollectorAuth.IsCollectorRequest(ctx));
        }

        [Fact]
        public void IsCollectorRequest_ValidToken_ReturnsTrue()
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers[HealthCheckCollectorAuth.HeaderName] = HealthCheckCollectorAuth.Token;
            Assert.True(HealthCheckCollectorAuth.IsCollectorRequest(ctx));
        }

        [Fact]
        public void IsCollectorRequest_HeaderNameIsCaseInsensitive()
        {
            // ASP.NET request headers are case-insensitive; the filter must accept any casing
            // a proxy might apply.
            var ctx = new DefaultHttpContext();
            ctx.Request.Headers["x-health-collector-token"] = HealthCheckCollectorAuth.Token;
            Assert.True(HealthCheckCollectorAuth.IsCollectorRequest(ctx));
        }
    }
}
