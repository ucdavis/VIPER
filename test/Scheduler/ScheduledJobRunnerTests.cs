using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Viper.Areas.Scheduler.Models;
using Viper.Areas.Scheduler.Services;

namespace Viper.test.Scheduler
{
    public sealed class ScheduledJobRunnerTests
    {
        // Captures the ScheduledJobContext the runner hands the job, so tests
        // can assert that ModBy and TriggerSource are set correctly.
        private sealed class CapturingJob : IScheduledJob
        {
            public ScheduledJobContext? CapturedContext { get; private set; }
            public bool Ran { get; private set; }

            public Task RunAsync(ScheduledJobContext context, CancellationToken ct)
            {
                CapturedContext = context;
                Ran = true;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task RunAsync_ResolvesJobByIdAndStampsSchedulerActor()
        {
            var capturing = new CapturingJob();
            var services = new ServiceCollection();
            services.AddSingleton(capturing);

            var registry = new ScheduledJobRegistry(new Dictionary<string, ScheduledJobMetadata>
            {
                ["test:capturing"] = new ScheduledJobMetadata(
                    typeof(CapturingJob), "test:capturing", "0 0 * * *", "UTC", isSystem: false),
            });
            services.AddSingleton<IScheduledJobRegistry>(registry);

            var provider = services.BuildServiceProvider();
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var logger = Substitute.For<ILogger<ScheduledJobRunner>>();
            var runner = new ScheduledJobRunner(scopeFactory, logger);

            var jobCt = Substitute.For<Hangfire.IJobCancellationToken>();
            jobCt.ShutdownToken.Returns(CancellationToken.None);
            await runner.RunAsync("test:capturing", jobCt, performContext: null);

            Assert.True(capturing.Ran);
            Assert.NotNull(capturing.CapturedContext);
            Assert.Equal(TriggerSource.Scheduled, capturing.CapturedContext!.TriggerSource);
            Assert.Equal(ISchedulerJobsService.SchedulerActor, capturing.CapturedContext.ModBy);
        }

        [Fact]
        public async Task RunAsync_LogsAndSkipsWhenIdUnknown()
        {
            var services = new ServiceCollection();
            var registry = new ScheduledJobRegistry(new Dictionary<string, ScheduledJobMetadata>());
            services.AddSingleton<IScheduledJobRegistry>(registry);
            var provider = services.BuildServiceProvider();
            var logger = Substitute.For<ILogger<ScheduledJobRunner>>();
            var runner = new ScheduledJobRunner(provider.GetRequiredService<IServiceScopeFactory>(), logger);

            var jobCt = Substitute.For<Hangfire.IJobCancellationToken>();
            jobCt.ShutdownToken.Returns(CancellationToken.None);
            var ex = await Record.ExceptionAsync(() => runner.RunAsync("does-not-exist", jobCt, performContext: null));
            Assert.Null(ex);
        }
    }
}
