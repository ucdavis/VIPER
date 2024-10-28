using NLog;
using Quartz;
using Quartz.Core;
using Quartz.Impl;

namespace Viper.Areas.Jobs
{
    public class JobManager : IHostedService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly NLog.Logger logger;
        public JobManager(IServiceProvider serviceProvider)
        {
            logger = LogManager.GetCurrentClassLogger();
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Configure();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task Configure()
        {
            try
            {
                var factory = new StdSchedulerFactory();
                var scheduler = await factory.GetScheduler();
                scheduler.JobFactory = new JobFactory(serviceProvider, scheduler);

                await ConfigureWorkers(scheduler);
                await ConfigureManager(scheduler);

                await scheduler.Start();
                await Task.Delay(TimeSpan.FromSeconds(1));
                logger.Info("Configured Job Manager");
            }
            catch(Exception e)
            {
                Logger l = LogManager.GetCurrentClassLogger();
                l.Error(e);
            }
        }

        public async Task<IScheduler> ConfigureManager(IScheduler scheduler)
        {
            // Configure Scheduler for Zookeeper
            var jmTriggerTime = "*/10 * * * * ? *";
            logger.Info($"Configuring JobManagerJob with Trigger {jmTriggerTime}");

            var jmJob = JobBuilder.Create<JobManagerJob>()
                .WithIdentity(JobManagerJob.JobName, JobManagerJob.JobGroup)
                .Build();

            var jmTrigger = TriggerBuilder.Create()
                .WithIdentity(JobManagerJob.TriggerName, JobManagerJob.TriggerGroup)
                .WithCronSchedule(jmTriggerTime)
                .Build();

            await scheduler.ScheduleJob(jmJob, jmTrigger);

            return scheduler;
        }

        async Task<IScheduler> ConfigureWorkers(IScheduler scheduler)
        {
            foreach (var worker in JobFactory.Workers)
            {
                scheduler = await worker.ConfigureScheduler(scheduler);
            }
            return scheduler;
        }
    }
}
