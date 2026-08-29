using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Web.Authorization;

namespace Viper.test.Classes
{
    /// <summary>
    /// Pins the deliberate Development bypass, since no Duo credential can be issued for a localhost
    /// callback, and pins that the failure message is set only when the requirement fails.
    /// </summary>
    public class DuoAuthenticationRequirementTests
    {
        private const string ErrorKey = "ErrorMessage";

        private static HttpContext ContextFor(string environmentName)
        {
            var env = Substitute.For<IWebHostEnvironment>();
            env.EnvironmentName = environmentName;

            var services = new ServiceCollection();
            services.AddSingleton(env);

            return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        }

        private static ClaimsPrincipal UserWithDuo() =>
            new(new ClaimsIdentity(new[] { new Claim("credentialType", "DuoCredential") }, "test"));

        private static async Task<(bool Succeeded, object? Error)> EvaluateAsync(ClaimsPrincipal user, string environmentName)
        {
            var httpContext = ContextFor(environmentName);
            var requirement = new DuoAuthenticationRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, httpContext);

            await requirement.HandleAsync(context);

            httpContext.Items.TryGetValue(ErrorKey, out object? error);
            return (context.HasSucceeded, error);
        }

        [Fact]
        public async Task DuoCredential_Succeeds()
        {
            var (succeeded, error) = await EvaluateAsync(UserWithDuo(), "Production");

            Assert.True(succeeded);
            Assert.Null(error);
        }

        [Fact]
        public async Task Development_SucceedsWithoutDuo()
        {
            var (succeeded, error) = await EvaluateAsync(new ClaimsPrincipal(new ClaimsIdentity()), "Development");

            Assert.True(succeeded);
            // The bypass is not a failure, so it must not leave a failure message behind.
            Assert.Null(error);
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Test")]
        public async Task OutsideDevelopment_FailsWithoutDuoAndExplainsWhy(string environmentName)
        {
            var (succeeded, error) = await EvaluateAsync(new ClaimsPrincipal(new ClaimsIdentity()), environmentName);

            Assert.False(succeeded);
            Assert.Equal("DUO two-factor authentication is required", error);
        }
    }
}

namespace Test.Classes
{
    // The "2faAuthentication" policy is the only gate on the app's most sensitive areas, so the
    // set of credential types it accepts is worth pinning: too narrow locks out Entra users who
    // did complete MFA, too wide lets a password-only sign-in through.
    public class DuoAuthenticationRequirementTests
    {
        private static ClaimsPrincipal PrincipalWith(params (string Type, string Value)[] claims)
            => new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "TestAuth"));

        [Theory]
        [InlineData("DuoCredential")]
        [InlineData("DuoSecurityUniversalPromptCredential")]
        [InlineData("DuoSecurityCredential")]
        public void HasDuoAuthentication_CasDuoCredentialTypes_ReturnTrue(string credentialType)
        {
            var user = PrincipalWith(("credentialType", credentialType));

            Assert.True(DuoAuthenticationRequirement.HasDuoAuthentication(user));
        }

        // Entra has no Duo attribute; EntraIdClaimMapper translates an "amr" multifactor into this
        // credential type, and the policy has to accept it or every Entra user fails 2FA.
        [Fact]
        public void HasDuoAuthentication_EntraIdMultifactorCredentialType_ReturnsTrue()
        {
            var user = PrincipalWith(("credentialType", EntraIdClaimMapper.MultifactorCredentialType));

            Assert.True(DuoAuthenticationRequirement.HasDuoAuthentication(user));
        }

        // End-to-end with the mapper, so a rename on either side of the translation fails here.
        [Fact]
        public void HasDuoAuthentication_PrincipalBuiltByMapperWithMultifactor_ReturnsTrue()
        {
            var user = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: true, DateTime.Now);

            Assert.True(DuoAuthenticationRequirement.HasDuoAuthentication(user));
        }

        [Fact]
        public void HasDuoAuthentication_PrincipalBuiltByMapperWithoutMultifactor_ReturnsFalse()
        {
            var user = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, DateTime.Now);

            Assert.False(DuoAuthenticationRequirement.HasDuoAuthentication(user));
        }

        [Fact]
        public void HasDuoAuthentication_SingleFactorCredentialType_ReturnsFalse()
        {
            var user = PrincipalWith(("credentialType", "UsernamePasswordCredential"));

            Assert.False(DuoAuthenticationRequirement.HasDuoAuthentication(user));
        }

        [Fact]
        public void HasDuoAuthentication_NoCredentialTypeClaim_ReturnsFalse()
        {
            var user = PrincipalWith((ClaimTypes.Name, "jdoe"));

            Assert.False(DuoAuthenticationRequirement.HasDuoAuthentication(user));
        }
    }
}
