// Joins the existing Web.Authorization cluster in this folder (CasSettings,
// ClaimsTransformer, PermissionAttribute) rather than the folder-derived Viper.Classes.
// ReSharper disable once CheckNamespace
namespace Web.Authorization
{
    /// <summary>
    /// Single sign-on providers the app can sign users in with.
    /// </summary>
    /// <remarks>
    /// An environment uses exactly one: campus switches from CAS to Entra ID in one step. Flags
    /// so a misconfigured "Cas, EntraId" still binds and is caught at startup, which falls back
    /// to CAS.
    /// </remarks>
    [Flags]
    public enum LoginProviders
    {
        None = 0,
        Cas = 1,
        EntraId = 2
    }
}
