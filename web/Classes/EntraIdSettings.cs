namespace Web.Authorization
{
    /// <summary>
    /// Microsoft Entra ID (OpenID Connect) settings, bound from the "EntraId" configuration section.
    /// </summary>
    /// <remarks>
    /// <see cref="ClientSecret"/> must never be committed to appsettings. It comes from AWS Systems
    /// Manager Parameter Store in TEST/PROD (/{Environment}/EntraId/ClientSecret) and from
    /// .env.local (EntraId__ClientSecret) in development, matching how the rest of the app's
    /// secrets are supplied.
    /// </remarks>
    public class EntraIdSettings
    {
        /// <summary>UC Davis Entra tenant id.</summary>
        public string? TenantId { get; set; }

        /// <summary>Application (client) id of the VIPER Enterprise App registration.</summary>
        public string? ClientId { get; set; }

        /// <summary>Client secret. Supplied out of band, never from a committed config file.</summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Redirect path Entra returns to. Registered as a redirect URI in the app registration.
        /// ASP.NET prefixes the PathBase automatically, so TEST registers "/2/signin-entra".
        /// </summary>
        public string CallbackPath { get; set; } = "/signin-entra";

        /// <summary>Path Entra returns to after a federated sign-out.</summary>
        public string SignedOutCallbackPath { get; set; } = "/signout-entra";

        /// <summary>
        /// Claim carrying the user's campus identity. Defaults to the standard OIDC
        /// "preferred_username" (e.g. "jdoe@ucdavis.edu"). Set to "onpremisessamaccountname"
        /// if that optional claim is enabled on the app registration, which yields the bare
        /// kerberos id with no stripping required.
        /// </summary>
        public string LoginIdClaim { get; set; } = "preferred_username";

        /// <summary>
        /// Strip "@domain" off the resolved claim so it matches the bare kerberos login id that
        /// AAUD stores and <c>ClaimsTransformer</c> looks users up by.
        /// </summary>
        public bool StripEmailDomain { get; set; } = true;

        /// <summary>Authority URL for the tenant, derived from <see cref="TenantId"/>.</summary>
        public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";

        /// <summary>True when there is enough configuration to register the OIDC handler.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);
    }
}
