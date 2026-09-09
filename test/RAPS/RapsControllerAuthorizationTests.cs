using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Viper.Areas.RAPS.Controllers;

namespace Viper.test.RAPS
{
    /// <summary>
    /// Every controller in the area has to sit behind both the RAPS roles and Duo. A gap is invisible in
    /// Development, where Duo auto-succeeds, and shows up on Test/Prod as a page whose API 403s.
    /// </summary>
    public class RapsControllerAuthorizationTests
    {
        public static TheoryData<Type> RapsControllers()
        {
            return new TheoryData<Type>(typeof(RAPSController).Assembly.GetTypes()
                .Where(t => t.Namespace == typeof(RAPSController).Namespace && t.Name.EndsWith("Controller"))
                .OrderBy(t => t.Name));
        }

        [Theory]
        [MemberData(nameof(RapsControllers))]
        public void EveryRapsController_RequiresDuoTwoFactor(Type controller)
        {
            var authorize = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .FirstOrDefault(a => a.Policy == "2faAuthentication");

            Assert.True(authorize is not null, $"{controller.Name} is missing [Authorize(Policy = \"2faAuthentication\")]");
        }

        [Theory]
        [MemberData(nameof(RapsControllers))]
        public void EveryRapsController_RequiresARapsRole(Type controller)
        {
            var roles = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .Select(a => a.Roles)
                .FirstOrDefault(r => !string.IsNullOrEmpty(r));

            Assert.False(string.IsNullOrEmpty(roles), $"{controller.Name} is missing an [Authorize(Roles = ...)] restriction");
        }

        [Fact]
        public void RapsControllersAreDiscovered()
        {
            // Guards the two theories above against silently passing on an empty set.
            Assert.NotEmpty(RapsControllers());
        }
    }
}
