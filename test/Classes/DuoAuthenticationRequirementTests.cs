using System.Security.Claims;
using Web.Authorization;

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
