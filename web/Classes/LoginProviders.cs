namespace Web.Authorization
{
    /// <summary>
    /// Single sign-on providers the app can offer on the welcome screen.
    /// </summary>
    /// <remarks>
    /// Flags, because campus is mid-migration from CAS to Entra ID and TEST needs to run both
    /// side by side. Configuration binding parses the member names, so "Cas", "EntraId",
    /// "Both", and "Cas, EntraId" are all valid values for Authentication:EnabledProviders.
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
