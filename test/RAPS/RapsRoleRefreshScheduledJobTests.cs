using System.Reflection;
using Viper.Areas.RAPS.Jobs;
using Viper.Areas.Scheduler.Services;

namespace Viper.test.RAPS
{
    public sealed class RapsRoleRefreshScheduledJobTests
    {
        [Fact]
        public void Class_IsDecoratedWithScheduledJob()
        {
            var attr = typeof(RapsRoleRefreshScheduledJob).GetCustomAttribute<ScheduledJobAttribute>();

            Assert.NotNull(attr);
            Assert.Equal("raps:role-refresh", attr.Id);
            Assert.Equal("0 0 * * *", attr.Cron);
            Assert.Equal("Pacific Standard Time", attr.TimeZoneId);
            Assert.False(attr.IsSystem);
        }

        [Fact]
        public void Class_DoesNotUseSystemPrefix()
        {
            // The RAPS role-refresh is a user job, not scheduler infrastructure.
            // Pause/resume must remain available, which the API blocks for
            // __scheduler:* ids.
            var attr = typeof(RapsRoleRefreshScheduledJob).GetCustomAttribute<ScheduledJobAttribute>();
            Assert.NotNull(attr);
            Assert.False(attr.Id.StartsWith(ISchedulerJobsService.SystemJobPrefix, StringComparison.Ordinal));
        }
    }
}
