using Microsoft.Extensions.DependencyInjection;
using Viper.Areas.Scheduler.Models;
using Viper.Areas.Scheduler.Services;

namespace Viper.test.Scheduler
{
    public sealed class ScheduledJobDiscoveryTests
    {
        [ScheduledJob(id: "test:good", cron: "0 0 * * *")]
        public sealed class GoodJob : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        [ScheduledJob(id: "test:second", cron: "0 1 * * *")]
        public sealed class SecondJob : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        public class JobMissingAttribute : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        [Fact]
        public void RegisterScheduledJobs_DiscoversValidJobs()
        {
            var services = new ServiceCollection();
            var found = ManualDiscover(services, new[] { typeof(GoodJob), typeof(SecondJob) });

            Assert.Equal(2, found.Count);
            Assert.Contains(found, m => m.Id == "test:good");
            Assert.Contains(found, m => m.Id == "test:second");
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
        public void RegisterScheduledJobs_ThrowsOnDuplicateId()
        {
            var services = new ServiceCollection();
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ManualDiscover(services, new[] { typeof(GoodJob), typeof(DuplicateIdJob) }));
            Assert.Contains("Duplicate", ex.Message);
        }

        [ScheduledJob(id: "test:good", cron: "0 0 * * *")]
        public sealed class DuplicateIdJob : IScheduledJob
        {
            public Task RunAsync(ScheduledJobContext context, CancellationToken ct) => Task.CompletedTask;
        }

        private static IReadOnlyList<ScheduledJobMetadata> ManualDiscover(
            IServiceCollection services,
            Type[] types)
        {
            var stub = new StubAssembly(types);
            return ScheduledJobDiscovery.RegisterScheduledJobs(services, new[] { stub });
        }

        private sealed class StubAssembly : System.Reflection.Assembly
        {
            private readonly Type[] _types;
            public StubAssembly(Type[] types) { _types = types; }
            public override Type[] GetTypes() => _types;
        }
    }
}
