using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.DirectoryServices.Protocols;
using Viper.Areas.Directory.Services;
using Viper.Areas.RAPS.Services;
using Viper.Areas.RAPS.Models.Uinform;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Xunit;
using Amazon;
using Amazon.Extensions.NETCore.Setup;

namespace Viper.test.Services
{
    public class UserInfoServiceTests
    {
        private readonly ITestOutputHelper _output;

        public UserInfoServiceTests(ITestOutputHelper output)
        {
            _output = output;
            Console.SetOut(new ConsoleRedirector(output));
        }

        [Fact]
        public async Task TestGetUserInfo()
        {
            IConfigurationRoot config;
            try
            {
                // Setup configuration using environment, appsettings, and SSM Parameter Store
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.Development.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                var awsOptions = new AWSOptions
                {
                    Region = RegionEndpoint.USWest1
                };
                var configBuilder = new ConfigurationBuilder()
                    .AddConfiguration(configuration)
                    .AddSystemsManager("/Development", awsOptions)
                    .AddSystemsManager("/Shared", awsOptions);
                config = configBuilder.Build();
            }
            catch (Exception ex) when (ex.ToString().Contains("Amazon") || ex.ToString().Contains("EC2") || ex.ToString().Contains("Metadata") || ex.ToString().Contains("credential"))
            {
                _output.WriteLine($"[SKIPPED] AWS SSM Parameter Store is not available: {ex.Message}");
                return; // Gracefully pass/skip the test in CI/CD pipeline
            }

            try
            {
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(config);
                services.AddMemoryCache();
                services.AddHttpClient();

                // Register database contexts using connection strings from SSM config
                void RegisterContext<TContext>(string key) where TContext : DbContext
                {
                    var connStr = config.GetConnectionString(key);
                    if (string.IsNullOrEmpty(connStr))
                    {
                        throw new Exception($"ConnectionString for '{key}' is empty or missing!");
                    }
                    services.AddDbContext<TContext>(options => options.UseSqlServer(connStr));
                }

                RegisterContext<AAUDContext>("AAUD");
                RegisterContext<RAPSContext>("RAPS");
                RegisterContext<CoursesContext>("Courses");
                services.AddDbContext<EquipmentLoanContext>(options => options.UseSqlServer(config.GetConnectionString("VIPER")));
                services.AddDbContext<PPSContext>(options => options.UseSqlServer(config.GetConnectionString("VIPER")));
                services.AddDbContext<IDCardsContext>(options => options.UseSqlServer(config.GetConnectionString("VIPER")));
                services.AddDbContext<KeysContext>(options => options.UseSqlServer(config.GetConnectionString("VIPER")));

                services.AddScoped<UserInfoService>();

                var serviceProvider = services.BuildServiceProvider();
                HttpHelper.Configure(serviceProvider.GetRequiredService<IMemoryCache>(), config, null!, null!, null!, null!);

                // Test logic will call GetUserInfoAsync and populate AD/Instinct details

                var userInfoService = serviceProvider.GetRequiredService<UserInfoService>();

                // Query Mothra ID 00065542 (Brandon Edwards - 'be5')
                var result = await userInfoService.GetUserInfoAsync(null, "00065542");

                if (result == null)
                {
                    _output.WriteLine("[DEBUG] GetUserInfoAsync returned null!");
                    throw new Xunit.Sdk.XunitException("GetUserInfoAsync returned null");
                }

                _output.WriteLine($"[DEBUG] IamId: '{result.IamId}'");
                _output.WriteLine($"[DEBUG] DisplayName: '{result.DisplayFullName}'");
                _output.WriteLine($"[DEBUG] InstinctId: '{result.InstinctId}'");
                _output.WriteLine($"[DEBUG] InstinctUsername: '{result.InstinctUsername}'");
                _output.WriteLine($"[DEBUG] InstinctStatus: '{result.InstinctStatus}'");
                _output.WriteLine($"[DEBUG] InstinctIsActive: {result.InstinctIsActive}");
                
                _output.WriteLine($"[DEBUG] ADDisplayName: '{result.ADDisplayName}'");
                _output.WriteLine($"[DEBUG] ADMail: '{result.ADMail}'");
                _output.WriteLine($"[DEBUG] ADSamAccountName: '{result.ADSamAccountName}'");
                _output.WriteLine($"[DEBUG] ADUserPrincipalName: '{result.ADUserPrincipalName}'");
                _output.WriteLine($"[DEBUG] ADDistinguishedName: '{result.ADDistinguishedName}'");
                _output.WriteLine($"[DEBUG] ADMemberOf count: {result.ADMemberOf?.Count ?? 0}");
                if (result.ADMemberOf != null)
                {
                    foreach (var group in result.ADMemberOf)
                    {
                        _output.WriteLine($"  Group: '{group}'");
                    }
                }
                
                if (result.InstinctRoles != null)
                {
                    _output.WriteLine($"[DEBUG] InstinctRoles: {string.Join(", ", result.InstinctRoles)}");
                }

                if (result.InstinctInfo != null && !string.IsNullOrEmpty(result.InstinctInfo.ErrorMessage))
                {
                    _output.WriteLine($"[DEBUG] Instinct API Error: {result.InstinctInfo.ErrorMessage}");
                }

                Assert.NotNull(result.InstinctId);
                Assert.Equal("be5", result.InstinctUsername);
            }
            catch (Exception ex) when (ex.ToString().Contains("SqlException") || ex.ToString().Contains("network-related") || ex.ToString().Contains("login failed") || ex.ToString().Contains("LdapException") || ex.ToString().Contains("Active Directory"))
            {
                _output.WriteLine($"[SKIPPED] Database or network resources not accessible in this environment: {ex.Message}");
                return; // Gracefully pass/skip the test in CI/CD pipeline
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[DEBUG] Test execution failed with exception: {ex}");
                throw;
            }
        }

        private class ConsoleRedirector : TextWriter
        {
            private readonly ITestOutputHelper _output;
            public ConsoleRedirector(ITestOutputHelper output) => _output = output;
            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
            public override void WriteLine(string? value) => _output.WriteLine(value ?? "");
            public override void Write(string? value) => _output.WriteLine(value ?? "");
        }
    }
}
