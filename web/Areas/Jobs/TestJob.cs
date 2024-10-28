using NLog;
using Quartz;

namespace Viper.Areas.Jobs
{
    public class TestJob : WorkerJob
    {
        private readonly Logger logger;
        public TestJob() : base("TestJob", "TestJobGroup", "TestTrigger", "TestTriggerGroup", typeof(TestJob))
        {
            logger = LogManager.GetCurrentClassLogger();
        }

        public override Task Execute(IJobExecutionContext context)
        {
            logger.Info("Task TestJob started.");
            return Task.CompletedTask;
        }

        public override IJob? GetJob(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<TestJob>();
        }
    }
}
