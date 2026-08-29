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

        // Default settings read only the mapped kerberos claim. A token carrying just the standard
        // claims must not sign anyone in: preferred_username is the campus email alias, and an
        // alias that collides with someone else's kerberos id would resolve to the wrong user.
        [Fact]
        public void ResolveLoginId_DefaultSettings_IgnoresPreferredUsername()
        {
            var principal = PrincipalWith(("preferred_username", "jdoe@ucdavis.edu"));

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, DefaultSettings());

            Assert.Null(loginId);
        }

        [Fact]
        public void ResolveLoginId_ConfiguredEmailStyleClaim_StripsEmailDomain()
        {
            var principal = PrincipalWith(("preferred_username", "jdoe@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "preferred_username" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Equal("jdoe", loginId);
        }

        // When both arrive, only the configured kerberos claim is read, even though stripping the
        // alias would also produce a plausible-looking value.
        [Fact]
        public void ResolveLoginId_DefaultSettings_ReadsSamAccountNameNotPreferredUsername()
        {
            var principal = PrincipalWith(
                ("preferred_username", "jdoe-alias@ucdavis.edu"),
                ("onpremisessamaccountname", "jdoe"));

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, DefaultSettings());

            Assert.Equal("jdoe", loginId);
        }

        [Fact]
        public void ResolveLoginId_StripEmailDomainFalse_ReturnsClaimUnchanged()
        {
            var principal = PrincipalWith(("preferred_username", "jdoe@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "preferred_username", StripEmailDomain = false };

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

        // No fallback: an account without the mapped claim (cloud-only, unsynced) is rejected
        // rather than signed in as its email alias.
        [Fact]
        public void ResolveLoginId_ConfiguredClaimAbsent_ReturnsNull()
        {
            var principal = PrincipalWith(("preferred_username", "jdoe@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "onpremisessamaccountname" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Null(loginId);
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
        public void ResolveLoginId_ConfiguredClaimBlank_ReturnsNull()
        {
            // A blank configured claim counts as absent, and no other claim is consulted, even one
            // that looks usable.
            var principal = PrincipalWith(
                ("onpremisessamaccountname", "   "),
                ("upn", "jdoe@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "onpremisessamaccountname" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Null(loginId);
        }

        [Fact]
        public void ResolveLoginId_EmptyLocalPart_ReturnsNull()
        {
            // "@ucdavis.edu" has no local part to strip down to. Returning it whole would sign the
            // user in with an id matching nobody in AAUD, so it must resolve to null instead.
            var principal = PrincipalWith(("preferred_username", "@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "preferred_username" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

            Assert.Null(loginId);
        }

        [Fact]
        public void ResolveLoginId_EmptyLocalPart_StripDisabled_ReturnsValueUnchanged()
        {
            // With stripping off the value is passed through verbatim, whatever it looks like.
            var principal = PrincipalWith(("preferred_username", "@ucdavis.edu"));
            var settings = new EntraIdSettings { LoginIdClaim = "preferred_username", StripEmailDomain = false };

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
            var settings = new EntraIdSettings { LoginIdClaim = "preferred_username" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

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
            var settings = new EntraIdSettings { LoginIdClaim = "preferred_username" };

            var loginId = EntraIdClaimMapper.ResolveLoginId(principal, settings);

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
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAt: DateTime.Now);

            Assert.Equal("jdoe", principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal("jdoe", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }

        [Fact]
        public void BuildPrincipal_SetsAuthenticationMethod()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAt: DateTime.Now);

            Assert.Equal(EntraIdClaimMapper.AuthenticationMethod, principal.FindFirst(ClaimTypes.AuthenticationMethod)?.Value);
        }

        [Fact]
        public void BuildPrincipal_HasMultifactorTrue_AddsCredentialTypeClaim()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: true, authenticatedAt: DateTime.Now);

            Assert.Equal(EntraIdClaimMapper.MultifactorCredentialType, principal.FindFirst("credentialType")?.Value);
        }

        [Fact]
        public void BuildPrincipal_HasMultifactorFalse_AddsNoCredentialTypeClaim()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAt: DateTime.Now);

            Assert.Null(principal.FindFirst("credentialType"));
        }

        [Fact]
        public void BuildPrincipal_ResultingIdentity_IsAuthenticated()
        {
            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAt: DateTime.Now);

            Assert.True(principal.Identity?.IsAuthenticated);
        }

        [Fact]
        public void BuildPrincipal_WritesAuthenticationDateInRoundTripFormat()
        {
            var authenticatedAt = new DateTime(2026, 8, 6, 12, 34, 56, DateTimeKind.Local);

            var principal = EntraIdClaimMapper.BuildPrincipal("jdoe", hasMultifactor: false, authenticatedAt);

            Assert.Equal(authenticatedAt.ToString("o"), principal.FindFirst("authenticationDate")?.Value);
        }

        #endregion
    }
}
