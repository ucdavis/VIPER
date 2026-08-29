// Joins the existing Web.Authorization cluster in this folder (CasSettings,
// ClaimsTransformer, PermissionAttribute) rather than the folder-derived Viper.Classes.
// ReSharper disable once CheckNamespace
namespace Web.Authorization
{
    /// <summary>
    /// Which login provider the app uses, bound from the "Authentication" configuration section.
    /// </summary>
    public class AuthenticationSettings
    {
        /// <summary>
        /// The provider users sign in with. Defaults to CAS so an environment that has not been
        /// given an explicit setting keeps its pre-Entra behavior.
        /// </summary>
        public LoginProviders EnabledProviders { get; set; } = LoginProviders.Cas;

        public bool CasEnabled => EnabledProviders.HasFlag(LoginProviders.Cas);

        public bool EntraIdEnabled => EnabledProviders.HasFlag(LoginProviders.EntraId);
    }
}
