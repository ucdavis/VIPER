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
        // Constants, not settings: the configuration binder ignores consts, so nothing below
        // this block can be overridden from appsettings the way the properties that follow can.

        /// <summary>Configuration section these settings bind from.</summary>
        public const string SectionName = "EntraId";

        /// <summary>
        /// Path Entra calls, in a hidden iframe, when the user signs out anywhere in the tenant.
        /// Registered as the app registration's single "Front-channel logout URL" (TEST registers
        /// "https://&lt;host&gt;/2/frontchannel-logout"; the PathBase is applied by routing).
        /// </summary>
        /// <remarks>
        /// A const rather than a setting: it is a route attribute on
        /// <c>EntraLogoutController</c> and the CSP framing exemption in <c>Program.cs</c> keys off
        /// the same value, so the two cannot be allowed to drift.
        /// </remarks>
        public const string FrontChannelLogoutPath = "/frontchannel-logout";

        /// <summary>Named <see cref="HttpClient"/> used to forward front-channel logouts.</summary>
        public const string FrontChannelLogoutClientName = "EntraFrontChannelLogout";

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
        /// Absolute URL notified server-side when a front-channel logout arrives, in practice
        /// VIPER 1's "/public/entra/frontchannel-logout.cfm".
        /// </summary>
        /// <remarks>
        /// An app registration has room for exactly one front-channel logout URL and VIPER 1 shares
        /// this registration, so VIPER 1 cannot hear from Entra directly. VIPER 2 owns the URL and
        /// relays. Blank (the default) simply skips the relay, which is what a developer machine
        /// with no VIPER 1 running wants.
        /// </remarks>
        public string? FrontChannelLogoutForwardTo { get; set; }

        /// <summary>
        /// How long to wait on the relay. Short on purpose: the caller is an iframe Entra is
        /// blocking on, and a VIPER 1 that cannot answer promptly is not worth stalling sign-out
        /// everywhere else for.
        /// </summary>
        public int FrontChannelLogoutTimeoutSeconds { get; set; } = 5;

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

        /// <summary>
        /// Sent as "domain_hint" on every sign-in so campus users skip Microsoft's home-realm
        /// step (typing an email address before being handed to the campus identity provider).
        /// Matches VIPER 1's <c>AuthSettings.cfc</c> default.
        /// </summary>
        /// <remarks>
        /// It also limits the account picker to this domain, so an admin account in another
        /// verified domain such as ad3.ucdavis.edu is not offered. Set to "" to get the full
        /// picker back.
        /// </remarks>
        public string DomainHint { get; set; } = "ucdavis.edu";

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
