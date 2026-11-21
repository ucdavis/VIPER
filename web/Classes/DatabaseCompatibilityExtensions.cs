namespace Viper.Classes
{
    public static class DatabaseCompatibilityExtensions
    {
        // Lowest level among the databases EF connects to, and safe if config is missing:
        // a pin below a database's real level is harmless, while one above it can generate invalid SQL.
        private const int FallbackLevel = 130;

        /// <summary>
        /// Gets the compat level EF should target for a connection string name, from
        /// DatabaseCompatibilityLevels in appsettings. Raise a database before raising its value here.
        /// </summary>
        public static int GetCompatibilityLevel(this IConfiguration configuration, string connectionStringName)
        {
            var levels = configuration.GetSection("DatabaseCompatibilityLevels");
            return levels.GetValue<int?>(connectionStringName)
                ?? levels.GetValue<int?>("Default")
                ?? FallbackLevel;
        }
    }
}
