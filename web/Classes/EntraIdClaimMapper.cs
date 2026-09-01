using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Web.Authorization
{
    /// <summary>
    /// Translates an Entra ID principal into the claim shape the rest of VIPER already expects
    /// from a CAS login.
    /// </summary>
    /// <remarks>
    /// Two contracts have to be honored or downstream code silently breaks:
    /// <list type="bullet">
    /// <item><c>ClaimTypes.Name</c> must be the bare kerberos login id. <c>ClaimsTransformer</c>
    /// feeds it straight to <c>UserHelper.GetByLoginId</c> to resolve the AAUD user and its roles,
    /// so an email address here means no roles and no permissions.</item>
    /// <item>Two-factor is asserted through a <c>credentialType</c> claim. CAS emits a Duo value;
    /// Entra reports it in <c>amr</c> instead, so it is translated here and
    /// <c>DuoAuthenticationRequirement</c> accepts the translated value.</item>
    /// </list>
    /// Only these claims are carried over. The raw Entra token is deliberately not copied into the
    /// cookie: it is large enough to risk overflowing the 4KB cookie limit, and nothing reads it.
    /// </remarks>
    public static class EntraIdClaimMapper
    {
        /// <summary>Authentication scheme name for the OpenID Connect handler.</summary>
        public const string AuthenticationScheme = "EntraId";

        /// <summary>Value written to <see cref="ClaimTypes.AuthenticationMethod"/> for Entra logins.</summary>
        public const string AuthenticationMethod = "EntraId";

        /// <summary>
        /// <c>credentialType</c> value standing in for Duo when Entra reports a multifactor sign-in.
        /// </summary>
        /// <remarks>
        /// Conditional on <c>amr</c> because campus grants Duo exceptions: a password-only sign-in
        /// must fail the 2FA policy exactly as a Duo-less CAS session would.
        /// </remarks>
        public const string MultifactorCredentialType = "EntraIdMultifactorCredential";

        /// <summary>
        /// Resolves the campus kerberos login id from an Entra principal, or null when the
        /// configured claim is absent or blank. There is deliberately no fallback to
        /// preferred_username, upn or email: at UC Davis those carry the campus email alias, and
        /// signing in as an alias yields either zero roles or, on a collision with someone else's
        /// kerberos id, the wrong AAUD user. Failing closed is the safer outcome.
        /// </summary>
        public static string? ResolveLoginId(ClaimsPrincipal? principal, EntraIdSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (principal == null)
            {
                return null;
            }

            var raw = FirstNonEmptyClaim(principal, settings.LoginIdClaim);

            if (raw == null)
            {
                return null;
            }

            var loginId = raw.Trim();

            if (settings.StripEmailDomain)
            {
                var at = loginId.IndexOf('@', StringComparison.Ordinal);

                // A leading "@" leaves no local part to use. Returning it whole would sign the
                // user in with an id that matches nobody in AAUD, so treat it as unresolvable.
                if (at == 0)
                {
                    return null;
                }

                if (at > 0)
                {
                    loginId = loginId[..at];
                }
            }

            if (string.IsNullOrWhiteSpace(loginId))
            {
                return null;
            }

            // Lowercased to match what CAS supplies, because two places compare this value with an
            // ordinal, case-sensitive ==: UserHelper.IsInRole against AaudUser.LoginId, and the
            // emulation cache key, which ClaimsTransformer builds from this claim but EmulateUser
            // builds from AaudUser.LoginId. An Entra UPN with different casing would silently break
            // emulation and force a DB round trip on every role check.
            return loginId.ToLowerInvariant();
        }

        /// <summary>
        /// True when Entra reported a multifactor authentication for this sign-in.
        /// </summary>
        /// <remarks>
        /// <c>amr</c> ("authentication methods references") is an array in a v2.0 id_token, so it
        /// arrives as repeated claims. "mfa" is the standard value; "ngcmfa" appears for a freshly
        /// proofed credential. A Duo-backed campus sign-in was observed to carry ["pwd", "mfa"]
        /// (2026-08-31). The claim only exists because the app registration's manifest requests it:
        /// optionalClaims.idToken must contain { "name": "amr" }; without that entry v2.0 tokens
        /// omit amr entirely and every Entra session would fail the 2FA policy.
        /// </remarks>
        public static bool HasMultifactorAuthentication(ClaimsPrincipal? principal)
        {
            if (principal == null)
            {
                return false;
            }

            return principal.FindAll("amr")
                .Any(c => string.Equals(c.Value, "mfa", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(c.Value, "ngcmfa", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Builds the cookie principal for an Entra login, matching the claim shape
        /// <c>AuthenticateCasLogin</c> produces.
        /// </summary>
        public static ClaimsPrincipal BuildPrincipal(string loginId, bool hasMultifactor, DateTime authenticatedAt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(loginId);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, loginId),
                new(ClaimTypes.NameIdentifier, loginId),
                new(ClaimTypes.AuthenticationMethod, AuthenticationMethod),
                // Mirrors the CAS attribute of the same name, which the session UI surfaces. CAS
                // supplies a local time with an offset, so this stays local rather than emitting a
                // "Z" timestamp the two providers would not share.
                new("authenticationDate", authenticatedAt.ToString("o"))
            };

            if (hasMultifactor)
            {
                claims.Add(new Claim("credentialType", MultifactorCredentialType));
            }

            return new ClaimsPrincipal(
                new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        private static string? FirstNonEmptyClaim(ClaimsPrincipal principal, string? claimType)
        {
            if (string.IsNullOrWhiteSpace(claimType))
            {
                return null;
            }

            return principal.FindAll(claimType)
                .Select(c => c.Value)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }
    }
}
