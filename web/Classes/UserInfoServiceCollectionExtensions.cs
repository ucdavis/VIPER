using Microsoft.EntityFrameworkCore;
using Viper.Areas.Directory.Services;
using Viper.Classes.SQLContext;

namespace Viper.Classes
{
    /// <summary>
    /// DI registrations for the UserInfo feature: the EF contexts it
    /// aggregates (equipment loans, ID cards, keys, PPS - each its own
    /// database, not the VIPER app database), the HttpClient factory it
    /// depends on, and the service itself. Kept out of Program.cs so Main
    /// stays small.
    /// </summary>
    public static class UserInfoServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the EF contexts backing the UserInfo aggregation view
        /// (each against its own named connection string, compat level 130
        /// per our SQL Server 2016 target), plus its HttpClient factory and
        /// UserInfoService.
        /// </summary>
        public static WebApplicationBuilder AddUserInfoServices(this WebApplicationBuilder builder, bool enableDetailedErrors)
        {
            void RegisterContext<TContext>(string connectionStringName) where TContext : DbContext
            {
                var connStr = builder.Configuration.GetConnectionString(connectionStringName)
                    ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not configured");
                builder.Services.AddDbContext<TContext>(options =>
                {
                    options.UseSqlServer(connStr, o => o.UseCompatibilityLevel(130));
                    if (enableDetailedErrors) options.EnableDetailedErrors();
                });
            }

            RegisterContext<EquipmentLoanContext>("EquipmentLoan");
            RegisterContext<IDCardsContext>("IDCards");
            RegisterContext<KeysContext>("Keys");
            RegisterContext<PPSContext>("PPS");

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<UserInfoService>();

            return builder;
        }
    }
}
