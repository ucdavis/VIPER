// Joins the existing Web.Authorization cluster in this folder (CasSettings,
// ClaimsTransformer, PermissionAttribute) rather than the folder-derived Viper.Classes.
// ReSharper disable once CheckNamespace
namespace Web.Authorization
{
    /// <summary>
    /// Single sign-on providers the app can offer on the welcome screen.
    /// </summary>
    /// <remarks>
    /// Flags so the splash can offer both side by side for local development and testing;
    /// deployed environments run one. Configuration binding parses the member names, so "Cas",
    /// "EntraId", "Both", and "Cas, EntraId" are all valid values for
    /// Authentication:EnabledProviders.
    /// </remarks>
    [Flags]
    public enum LoginProviders
    {
        None = 0,
        Cas = 1,
        EntraId = 2,

        /// <summary>Both providers offered at once. Named for readability in appsettings.</summary>
        Both = Cas | EntraId
    }
}
