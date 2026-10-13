using Microsoft.AspNetCore.DataProtection;

namespace Viper.Classes
{
    /// <summary>
    /// Data Protection key ring wiring. Kept out of Program.cs so Main stays
    /// small.
    /// </summary>
    public static class DataProtectionExtensions
    {
        private const string KeyRingPathKey = "DataProtection:KeyRingPath";

        /// <summary>
        /// Registers Data Protection services (i.e. encryption). Both
        /// blue/green slots must share one key ring so a cookie minted by one
        /// slot decrypts in the other, so <c>DataProtection:KeyRingPath</c>
        /// points both slots at the same folder. Keys are protected with
        /// machine-scoped DPAPI, so they survive an app pool identity change
        /// but do not travel to the hot-spare: a cutover there costs each user
        /// one re-login through CAS. The path is unset locally, so dev keeps
        /// the default per-machine key location.
        /// </summary>
        public static IServiceCollection AddViperDataProtection(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var dataProtection = services.AddDataProtection()
                .SetApplicationName("VIPER2");

            var keyRingPath = configuration[KeyRingPathKey];
            if (!string.IsNullOrWhiteSpace(keyRingPath))
            {
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));

                // DPAPI is Windows-only, and the deployed slots are IIS on
                // Windows. The check keeps a configured key ring path working
                // everywhere else (e.g. CI) instead of being silently ignored.
                if (OperatingSystem.IsWindows())
                {
                    dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
                }
            }

            return services;
        }
    }
}
