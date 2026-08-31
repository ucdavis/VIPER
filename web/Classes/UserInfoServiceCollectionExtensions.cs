using Microsoft.EntityFrameworkCore;
using Viper.Areas.Directory.Services;
using Viper.Classes.SQLContext;

namespace Viper.Classes
{
    /// <summary>
    /// DI registrations for the UserInfo feature: the VIPER-database EF
    /// contexts it aggregates (equipment loans, ID cards, keys, PPS), the
    /// HttpClient factory it depends on, and the service itself. Kept out
    /// of Program.cs so Main stays small.
    /// </summary>
    public static class UserInfoServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the EF contexts backing the UserInfo aggregation view
        /// (all VIPER database, compat level 130 per our SQL Server 2016
        /// target), plus its HttpClient factory and UserInfoService.
        /// </summary>
        public static WebApplicationBuilder AddUserInfoServices(this WebApplicationBuilder builder, bool enableDetailedErrors)
        {
            void RegisterViperContext<TContext>() where TContext : DbContext
            {
                var connStr = builder.Configuration.GetConnectionString("VIPER")
                    ?? throw new InvalidOperationException("Connection string 'VIPER' not configured");
                builder.Services.AddDbContext<TContext>(options =>
                {
                    options.UseSqlServer(connStr, o => o.UseCompatibilityLevel(130));
                    if (enableDetailedErrors) options.EnableDetailedErrors();
                });
            }

            RegisterViperContext<EquipmentLoanContext>();
            RegisterViperContext<IDCardsContext>();
            RegisterViperContext<KeysContext>();
            RegisterViperContext<PPSContext>();

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<UserInfoService>();

            return builder;
        }
    }
}
