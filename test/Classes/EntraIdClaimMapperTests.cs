using System.Security.Claims;
using Web.Authorization;

namespace Test.Classes
{
    public class EntraIdClaimMapperTests
    {
        private static ClaimsPrincipal PrincipalWith(params (string Type, string Value)[] claims)
            => new(new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "TestAuth"));

        private static EntraIdSettings DefaultSettings() => new();

        #region ResolveLoginId

        [Fact]
        public void ResolveLoginId_DefaultSettings_StripsEmailDomain()
        {
            var principal = PrincipalWith(("preferred_username", "jdoe@ucdavis.edu"));

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, DefaultSettings());

            Assert.Equal("jdoe", loginId);
        }

        [Fact]
        public void ResolveLoginId_StripEmailDomainFalse_ReturnsClaimUnchanged()
        {
            var principal = PrincipalWith(("preferred_username", "jdoe@ucdavis.edu"));
            var settings = new EntraIdSettings { StripEmailDomain = false };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Equal("jdoe@ucdavis.edu", loginId);
        }

        [Fact]
        public void ResolveLoginId_ConfiguredClaimHasNoAtSign_ReturnsValueUnchanged()
        {
            var principal = PrincipalWith(("onpremisessamaccountname", "jdoe"));
            var settings = new EntraIdSettings { LoginIdClaim = "onpremisessamaccountname" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Equal("jdoe", loginId);
        }

        [Fact]
        public void ResolveLoginId_ConfiguredClaimAbsent_FallsBackToPreferredUsername()
        {
            var principal = PrincipalWith(("preferred_username", "jdoe@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "onpremisessamaccountname" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Equal("jdoe", loginId);
        }

        [Fact]
        public void ResolveLoginId_NoUsableClaims_ReturnsNull()
        {
            var principal = PrincipalWith(("some_other_claim", "whatever"));

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, DefaultSettings());

            Assert.Null(loginId);
        }

        [Fact]
        public void ResolveLoginId_NullPrincipal_ReturnsNull()
        {
            var loginId = EntraIdClaimMapper.ResolveLoginId(null, DefaultSettings());

            Assert.Null(loginId);
        }

        [Fact]
        public void ResolveLoginId_ConfiguredClaimEmpty_SkipsToFallbackWithValue()
        {
            // "preferred_username" is blank here, so resolution should move past it to the next
            // fallback (upn) rather than stopping on the empty value.
            var principal = PrincipalWith(
                ("preferred_username", "   "),
                ("upn", "jdoe@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "preferred_username" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Equal("jdoe", loginId);
        }

        [Fact]
        public void ResolveLoginId_EmptyLocalPart_ReturnsNull()
        {
            // "@ucdavis.edu" has no local part to strip down to. Returning it whole would sign the
            // user in with an id matching nobody in AAUD, so it must resolve to null instead.
            var principal = PrincipalWith(("preferred_username", "@ucdavis.edu"));

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, DefaultSettings());

            Assert.Null(loginId);
        }

        [Fact]
        public void ResolveLoginId_EmptyLocalPart_StripDisabled_ReturnsValueUnchanged()
        {
            // With stripping off the value is passed through verbatim, whatever it looks like.
            var principal = PrincipalWith(("preferred_username", "@ucdavis.edu"));
            var settings = new EntraIdSettings { StripEmailDomain = false };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Equal("@ucdavis.edu", loginId);
        }

        [Fact]
        public void ResolveLoginId_MixedCaseUpn_IsLowercased()
        {
            // AaudUser.LoginId is compared with an ordinal == in UserHelper.IsInRole, and the
            // emulation cache key is built from this value on one side and from AaudUser.LoginId on
            // the other, so Entra casing has to be normalized to what CAS supplies.
            var principal = PrincipalWith(("preferred_username", "JDoe@UCDavis.edu"));

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, DefaultSettings());

            Assert.Equal("jdoe", loginId);
        }

        [Fact]
        public void ResolveLoginId_MixedCaseSamAccountName_IsLowercased()
        {
            var principal = PrincipalWith(("onpremisessamaccountname", "JDoe"));
            var settings = new EntraIdSettings { LoginIdClaim = "onpremisessamaccountname" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Equal("jdoe", loginId);
        }

        [Fact]
        public void ResolveLoginId_SurroundingWhitespace_IsTrimmed()
        {
            var principal = PrincipalWith(("preferred_username", "  jdoe@ucdavis.edu  "));

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, DefaultSettings());

            Assert.Equal("jdoe", loginId);
        }

        #endregion

        #region HasMultifactorAuthentication

        [Fact]
        public void HasMultifactorAuthentication_AmrMfa_ReturnsTrue()
        {
            var principal = PrincipalWith(("amr", "mfa"));

            Assert.True(EntraIdClaimMapper.HasMultifactorAuthentication(principal));
        }

        [Fact]
        public void HasMultifactorAuthentication_AmrNgcMfa_ReturnsTrue()
        {
            var principal = PrincipalWith(("amr", "ngcmfa"));

            Assert.True(EntraIdClaimMapper.HasMultifactorAuthentication(principal));
        }

        [Fact]
        public void HasMultifactorAuthentication_AmrPwdOnly_ReturnsFalse()
        {
            var principal = PrincipalWith(("amr", "pwd"));

            Assert.False(EntraIdClaimMapper.HasMultifactorAuthentication(principal));
        }

        [Fact]
        public void HasMultifactorAuthentication_MultipleAmrClaimsIncludingMfa_ReturnsTrue()
        {
            // A v2.0 id_token's amr is an array, which arrives as repeated claims.
            var principal = PrincipalWith(("amr", "pwd"), ("amr", "mfa"));

            Assert.True(EntraIdClaimMapper.HasMultifactorAuthentication(principal));
        }

        [Fact]
        public void HasMultifactorAuthentication_CaseInsensitive_ReturnsTrue()
        {
            var principal = PrincipalWith(("amr", "MFA"));

            Assert.True(EntraIdClaimMapper.HasMultifactorAuthentication(principal));
        }

        [Fact]
        public void HasMultifactorAuthentication_NullPrincipal_ReturnsFalse()
        {
            Assert.False(EntraIdClaimMapper.HasMultifactorAuthentication(null));
        }

        #endregion

        #region BuildPrincipal

        [Fact]
        public void BuildPrincipal_SetsNameAndNameIdentifierToLoginId()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAtUtc: DateTime.UtcNow);

            Assert.Equal("jdoe", principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal("jdoe", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public void BuildPrincipal_SetsAuthenticationMethod()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAtUtc: DateTime.UtcNow);

            Assert.Equal(EntraIdClaimMapper.AuthenticationMethod, principal.FindFirst(ClaimTypes.AuthenticationMethod)?.Value);
        }

        [Fact]
        public void BuildPrincipal_HasMultifactorTrue_AddsCredentialTypeClaim()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: true, authenticatedAtUtc: DateTime.UtcNow);

            Assert.Equal(EntraIdClaimMapper.MultifactorCredentialType, principal.FindFirst("credentialType")?.Value);
        }

        [Fact]
        public void BuildPrincipal_HasMultifactorFalse_AddsNoCredentialTypeClaim()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAtUtc: DateTime.UtcNow);

            Assert.Null(principal.FindFirst("credentialType"));
        }

        [Fact]
        public void BuildPrincipal_ResultingIdentity_IsAuthenticated()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAtUtc: DateTime.UtcNow);

            Assert.True(principal.Identity?.IsAuthenticated);
        }

        [Fact]
        public void BuildPrincipal_WritesAuthenticationDateInRoundTripFormat()
        {
            var authenticatedAtUtc = new DateTime(2026, 8, 6, 12, 34, 56, DateTimeKind.Utc);

            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAtUtc);

            Assert.Equal(authenticatedAtUtc.ToString("o"), principal.FindFirst("authenticationDate")?.Value);
        }

        #endregion
    }
}
