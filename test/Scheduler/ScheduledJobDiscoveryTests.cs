using Microsoft.Extensions.DependencyInjection;
using Viper.Areas.Scheduler.Models;
using Viper.Areas.Scheduler.Services;

namespace Viper.test.Scheduler
{
    public sealed class ScheduledJobDiscoveryTests
    {
        // ── Sample IScheduledJob types used purely as discovery targets. ──

        [ScheduledJob(id: "test:good", cron: "0 0 * * *")]
        public sealed class GoodJob : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        [ScheduledJob(id: "__scheduler:legit-system", cron: "*/5 * * * *", IsSystem = true)]
        public sealed class GoodSystemJob : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        [ScheduledJob(id: "test:second", cron: "0 1 * * *")]
        public sealed class SecondJob : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        // Each negative case is its own assembly via a single-type assembly load
        // would be overkill - use an explicit type list instead.
        public class JobMissingAttribute : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        [ScheduledJob(id: "no-prefix-system", cron: "0 0 * * *", IsSystem = true)]
        public sealed class SystemJobWithoutPrefix : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        [ScheduledJob(id: "__scheduler:user-job", cron: "0 0 * * *")]
        public sealed class UserJobUsingSystemPrefix : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        // ── Tests ──

        [Fact]
        public void RegisterScheduledJobs_DiscoversValidJobs()
        {
            var services = new ServiceCollection();
            // The whole assembly contains negative-test types that would throw,
            // so the test isolates a curated type list via the StubAssembly
            // helper instead of scanning the real assembly.
            var found = ManualDiscover(services, new[] { typeof(GoodJob), typeof(GoodSystemJob), typeof(SecondJob) });

            Assert.Equal(3, found.Count);
            Assert.Contains(found, m => m.Id == "test:good" && !m.IsSystem);
            Assert.Contains(found, m => m.Id == "__scheduler:legit-system" && m.IsSystem);
        }

        [Fact]
        public void RegisterScheduledJobs_ThrowsWhenAttributeMissing()
        {
            var services = new ServiceCollection();
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ManualDiscover(services, new[] { typeof(JobMissingAttribute) }));
            Assert.Contains("[ScheduledJob]", ex.Message);
        }

        [Fact]
        public void RegisterScheduledJobs_ThrowsWhenSystemJobLacksPrefix()
        {
            var services = new ServiceCollection();
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ManualDiscover(services, new[] { typeof(SystemJobWithoutPrefix) }));
            Assert.Contains("IsSystem=true", ex.Message);
        }

        [Fact]
        public void RegisterScheduledJobs_ThrowsWhenUserJobUsesSystemPrefix()
        {
            var services = new ServiceCollection();
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ManualDiscover(services, new[] { typeof(UserJobUsingSystemPrefix) }));
            Assert.Contains("__scheduler:", ex.Message);
        }

        [Fact]
        public void RegisterScheduledJobs_ThrowsOnDuplicateId()
        {
            var services = new ServiceCollection();
            // Both GoodJob and GoodJobAlias declare id "test:good".
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ManualDiscover(services, new[] { typeof(GoodJob), typeof(DuplicateIdJob) }));
            Assert.Contains("Duplicate", ex.Message);
        }

        [ScheduledJob(id: "test:good", cron: "0 0 * * *")]
        public sealed class DuplicateIdJob : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        // Helper: emulate the assembly-scanning behavior with an explicit
        // type list so each test isolates the types it cares about.
        private static IReadOnlyList<ScheduledJobMetadata> ManualDiscover(
            IServiceCollection services,
            Type[] types)
        {
            var stub = new StubAssembly(types);
            return ScheduledJobDiscovery.RegisterScheduledJobs(services, new[] { stub });
        }

        // Minimal Assembly subclass that exposes only the types we provide.
        // GetTypes is the only method ScheduledJobDiscovery calls.
        private sealed class StubAssembly : System.Reflection.Assembly
        {
            private readonly Type[] _types;
            public StubAssembly(Type[] types) { _types = types; }
            public override Type[] GetTypes() => _types;
        }
    }
}
