namespace Web.Authorization
{
    /// <summary>
    /// Microsoft Entra ID (OpenID Connect) settings, bound from the "EntraId" configuration section.
    /// </summary>
    /// <remarks>
    /// There is no client secret. The app is a public client (authorization code + PKCE), so the
    /// only registration identifiers are <see cref="TenantId"/> and <see cref="ClientId"/>, and
    /// neither is secret: both travel in the browser's authorize redirect. One registration serves
    /// every environment, so both come from AWS Systems Manager Parameter Store at /Shared/EntraId/,
    /// and appsettings leaves them empty so a missing parameter fails <see cref="IsConfigured"/>.
    /// </remarks>
    public class EntraIdSettings
    {
        /// <summary>UC Davis Entra tenant id.</summary>
        public string? TenantId { get; set; }

        /// <summary>Application (client) id of the VIPER Enterprise App registration.</summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Redirect path Entra returns to. Registered as a redirect URI in the app registration.
        /// ASP.NET prefixes the PathBase automatically, so TEST registers "/2/signin-entra".
        /// </summary>
        public string CallbackPath { get; set; } = "/signin-entra";

        /// <summary>Path Entra returns to after a federated sign-out.</summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-entra";

        /// <summary>
        /// Claim carrying the user's campus kerberos id. "onpremisessamaccountname" is a mapped
        /// claim (enterprise app, Attributes &amp; Claims, source user.onpremisessamaccountname,
        /// "Expose claim in JWT tokens" ticked) that yields the bare id, e.g. "rexl". The standard
        /// "preferred_username" is NOT a substitute: at UC Davis its local part is the email alias
        /// ("rvlorenzo@ucdavis.edu"), which AAUD does not key on. Only this claim is read; a token
        /// without it is rejected rather than signed in as the alias.
        /// </summary>
        public string LoginIdClaim { get; set; } = "onpremisessamaccountname";

        /// <summary>
        /// Strip "@domain" off the resolved claim so it matches the bare kerberos login id that
        /// AAUD stores and <c>ClaimsTransformer</c> looks users up by.
        /// </summary>
        public bool StripEmailDomain { get; set; } = true;

        /// <summary>Authority URL for the tenant, derived from <see cref="TenantId"/>.</summary>
        public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";

        /// <summary>
        /// App-specific OIDC discovery document. The enterprise app carries a claims-mapping policy
        /// (it is how the kerberos id reaches the token), and Entra signs such tokens with the app's
        /// own signing certificate instead of the tenant keys. Only the "?appid=" variant of the
        /// discovery document lists that key; the tenant-wide one fails validation with IDX10503.
        /// </summary>
        public string MetadataAddress => $"{Authority}/.well-known/openid-configuration?appid={ClientId}";

        /// <summary>True when there is enough configuration to register the OIDC handler.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId);
    }
}
