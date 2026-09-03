using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Web.Authorization;

namespace Viper.test.Classes
{
    /// <summary>
    /// Pins the deliberate Development bypass, since no Duo credential can be issued for a localhost
    /// callback, and pins that the failure message is set only when the requirement fails.
    /// </summary>
    public class DuoAuthenticationRequirementTests
    {
        private const string ErrorKey = "ErrorMessage";

        private static HttpContext ContextFor(string environmentName)
        {
            var env = Substitute.For<IWebHostEnvironment>();
            env.EnvironmentName = environmentName;

            var services = new ServiceCollection();
            services.AddSingleton(env);

            return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        }

        private static ClaimsPrincipal UserWithDuo() =>
            new(new ClaimsIdentity(new[] { new Claim("credentialType", "DuoCredential") }, "test"));

        private static async Task<(bool Succeeded, object? Error)> EvaluateAsync(ClaimsPrincipal user, string environmentName)
        {
            var httpContext = ContextFor(environmentName);
            var requirement = new DuoAuthenticationRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, httpContext);

            await requirement.HandleAsync(context);

            httpContext.Items.TryGetValue(ErrorKey, out object? error);
            return (context.HasSucceeded, error);
        }

        [Fact]
        public async Task DuoCredential_Succeeds()
        {
            var (succeeded, error) = await EvaluateAsync(UserWithDuo(), "Production");

            Assert.True(succeeded);
            Assert.Null(error);
        }

        [Fact]
        public async Task Development_SucceedsWithoutDuo()
        {
            var (succeeded, error) = await EvaluateAsync(new ClaimsPrincipal(new ClaimsIdentity()), "Development");

            Assert.True(succeeded);
            // The bypass is not a failure, so it must not leave a failure message behind.
            Assert.Null(error);
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Test")]
        public async Task OutsideDevelopment_FailsWithoutDuoAndExplainsWhy(string environmentName)
        {
            var (succeeded, error) = await EvaluateAsync(new ClaimsPrincipal(new ClaimsIdentity()), environmentName);

            Assert.False(succeeded);
            Assert.Equal("DUO two-factor authentication is required", error);
        }
    }
}
