using System.Reflection;
using Viper.Classes;
using Viper.Controllers;

namespace Viper.test.Controllers
{
    /// <summary>
    /// The session expiry poll must never extend the session, or it would never time out. Both
    /// controller bases in this app write a fresh expiry on every action, so the guarantee rests
    /// entirely on this controller not inheriting either of them. Pin that.
    /// </summary>
    public class SessionTimeoutControllerTests
    {
        [Fact]
        public void Controller_DoesNotInheritASessionExtendingBase()
        {
            Assert.False(typeof(ApiController).IsAssignableFrom(typeof(SessionTimeoutController)));
            Assert.False(typeof(AreaController).IsAssignableFrom(typeof(SessionTimeoutController)));
        }

        [Fact]
        public void Controller_DoesNotCarryTheSessionUpdateFilter()
        {
            Assert.Empty(typeof(SessionTimeoutController)
                .GetCustomAttributes(typeof(ApiSessionUpdateFilterAttribute), inherit: true));

            foreach (MethodInfo action in typeof(SessionTimeoutController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.Empty(action.GetCustomAttributes(typeof(ApiSessionUpdateFilterAttribute), inherit: true));
            }
        }
    }
}
