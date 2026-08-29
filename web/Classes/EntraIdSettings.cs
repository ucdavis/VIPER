// Joins the existing Web.Authorization cluster in this folder (CasSettings,
// ClaimsTransformer, PermissionAttribute) rather than the folder-derived Viper.Classes.
// ReSharper disable once CheckNamespace
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
        // Trimmed on set: these arrive from Parameter Store, where a stray newline would be baked
        // into the authority and discovery URLs and fail at the first sign-in.
        private string? _tenantId;
        private string? _clientId;

        /// <summary>UC Davis Entra tenant id.</summary>
        public string? TenantId
        {
            get => _tenantId;
            set => _tenantId = value?.Trim();
        }

        /// <summary>Application (client) id of the VIPER Enterprise App registration.</summary>
        public string? ClientId
        {
            get => _clientId;
            set => _clientId = value?.Trim();
        }

        /// <summary>
        /// Redirect path Entra returns to. Registered as a redirect URI in the app registration.
        /// ASP.NET prefixes the PathBase automatically, so TEST registers "/2/signin-entra".
        /// </summary>
        public string CallbackPath { get; set; } = "/signin-entra";

        /// <summary>Path Entra returns to after a federated sign-out.</summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-entra";

        /// <summary>
        /// Claim carrying the user's campus kerberos id. Only the configured claim is consulted;
        /// a token without it is rejected rather than signed in. The default,
        /// "onpremisessamaccountname", is a mapped claim (enterprise app, Attributes &amp; Claims,
        /// source user.onpremisessamaccountname, "Expose claim in JWT tokens" ticked) yielding the
        /// bare id, e.g. "rexl". Reconfiguring to an email-style claim such as "preferred_username"
        /// is possible but unsafe at UC Davis: its local part is the email alias
        /// ("rvlorenzo@ucdavis.edu"), which AAUD does not key on.
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
        /// <remarks>
        /// ClientId must be a dashed GUID: <see cref="MetadataAddress"/> passes it as "?appid=",
        /// and any other shape registers the handler and then dead-ends at discovery. TenantId is
        /// only checked for content, since Entra accepts a verified domain name there too.
        /// </remarks>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(TenantId)
            && Guid.TryParseExact(ClientId, "D", out _);
    }
}
