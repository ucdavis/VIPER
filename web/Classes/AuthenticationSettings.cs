namespace Web.Authorization
{
    /// <summary>
    /// Which login provider(s) the app offers, bound from the "Authentication" configuration section.
    /// </summary>
    public class AuthenticationSettings
    {
        /// <summary>
        /// Providers offered on the welcome screen. Defaults to CAS so an environment that has not
        /// been given an explicit setting keeps its pre-Entra behavior.
        /// </summary>
        public LoginProviders EnabledProviders { get; set; } = LoginProviders.Cas;

        public bool CasEnabled => EnabledProviders.HasFlag(LoginProviders.Cas);

        public bool EntraIdEnabled => EnabledProviders.HasFlag(LoginProviders.EntraId);

        /// <summary>
        /// True when the user has a choice to make, which is the only case where the welcome
        /// screen needs to show two buttons and /login cannot pick a provider on its own.
        /// </summary>
        public bool HasProviderChoice => CasEnabled && EntraIdEnabled;
    }
}
