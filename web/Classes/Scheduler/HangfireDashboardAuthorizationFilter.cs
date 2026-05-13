using Hangfire.Dashboard;
using Viper.Classes.SQLContext;

namespace Viper.Classes.Scheduler
{
    /// <summary>
    /// Gate the Hangfire dashboard on the same RAPS permission used by the
    /// scheduler API (<c>SVMSecure.CATS.scheduledJobs</c>).
    /// Unauthenticated users are handled upstream by
    /// <c>RequireAuthorization()</c> on the mapped endpoint, which triggers
    /// the cookie auth challenge and redirects to <c>/login</c> (and on to
    /// CAS); this filter therefore only sees authenticated principals and
    /// decides authorize-or-403 based on permission membership. When the
    /// filter returns false for an authenticated user, Hangfire's middleware
    /// writes a 403.
    /// </summary>
    public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public const string SchedulerPermission = "SVMSecure.CATS.scheduledJobs";

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var user = httpContext.User;

            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                return false;
            }

            var services = httpContext.RequestServices;
            var userHelper = services.GetService<IUserHelper>();
            var rapsContext = services.GetService<RAPSContext>();
            var aaudContext = services.GetService<AAUDContext>();

            if (userHelper == null || rapsContext == null || aaudContext == null)
            {
                return false;
            }

            var loginId = user.Identity.Name;
            if (string.IsNullOrEmpty(loginId))
            {
                return false;
            }

            var aaudUser = userHelper.GetByLoginId(aaudContext, loginId);
            return aaudUser != null
                && userHelper.HasPermission(rapsContext, aaudUser, SchedulerPermission);
        }
    }
}
