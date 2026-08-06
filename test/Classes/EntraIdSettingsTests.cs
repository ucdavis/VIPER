using Web.Authorization;

namespace Test.Classes
{
    // IsConfigured decides at startup whether the OIDC handler is registered at all. When it is
    // wrong in the permissive direction the app offers a sign-in button that dead-ends, which is
    // exactly the failure the startup guard exists to prevent.
    public class EntraIdSettingsTests
    {
        private static EntraIdSettings Configured() => new()
        {
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret"
        };

        [Fact]
        public void IsConfigured_AllRequiredValuesPresent_ReturnsTrue()
        {
            Assert.True(Configured().IsConfigured);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsConfigured_TenantIdMissing_ReturnsFalse(string? tenantId)
        {
            var settings = Configured();
            settings.TenantId = tenantId;

            Assert.False(settings.IsConfigured);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsConfigured_ClientIdMissing_ReturnsFalse(string? clientId)
        {
            var settings = Configured();
            settings.ClientId = clientId;

            Assert.False(settings.IsConfigured);
        }

        // The secret is never committed: it arrives from SSM or .env.local. An environment that
        // has the rest of the settings but no secret is the expected pre-cutover state.
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsConfigured_ClientSecretMissing_ReturnsFalse(string? clientSecret)
        {
            var settings = Configured();
            settings.ClientSecret = clientSecret;

            Assert.False(settings.IsConfigured);
        }

        [Fact]
        public void IsConfigured_DefaultSettings_ReturnsFalse()
        {
            Assert.False(new EntraIdSettings().IsConfigured);
        }

        [Fact]
        public void Authority_UsesV2EndpointForTenant()
        {
            var settings = new EntraIdSettings { TenantId = "a8046f64-66c0-4f00-9046-c8daf92ff62b" };

            Assert.Equal(
                "https://login.microsoftonline.com/a8046f64-66c0-4f00-9046-c8daf92ff62b/v2.0",
                settings.Authority);
        }

        // The defaults are the deployed contract: the callback paths are registered as redirect
        // URIs in the app registration, and the claim/strip pair is what yields a bare kerberos id.
        [Fact]
        public void Defaults_MatchRegisteredRedirectUrisAndAaudLoginIdShape()
        {
            var settings = new EntraIdSettings();

            Assert.Equal("/signin-entra", settings.CallbackPath);
            Assert.Equal("/signout-entra", settings.SignedOutCallbackPath);
            Assert.Equal("preferred_username", settings.LoginIdClaim);
            Assert.True(settings.StripEmailDomain);
        }
    }
}
