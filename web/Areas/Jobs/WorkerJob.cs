using NLog;
using Quartz;

namespace Viper.Areas.Jobs
{
    public class WorkerJob : IJob
    {
        public string JobName { get; set; }
        public string JobGroup { get; set; }
        public string TriggerName { get; set; }
        public string TriggerGroup { get; set; }
        public JobKey JobKey
        {
            get
            {
                return new JobKey(JobName, JobGroup);
            }
        }
        public TriggerKey TriggerKey
        {
            get
            {
                return new TriggerKey(TriggerName, TriggerGroup);
            }
        }
        public Type JobType { get; set; }

        public WorkerJob(string jobName, string jobGroup, string triggerName, string triggerGroup, Type jobType)
        {
            JobName = jobName;
            JobGroup = jobGroup;
            TriggerName = triggerName;
            TriggerGroup = triggerGroup;
            JobType = jobType;
        }

        

        public virtual Task Execute(IJobExecutionContext context)
        {
            throw new NotImplementedException();
        }

        public virtual IJob? GetJob(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<WorkerJob>();
        }

        public async Task<IScheduler> ConfigureScheduler(IScheduler scheduler)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            var triggerTime = "0 * * ? * *";
            logger.Info($"Configuring Job {JobName} with Trigger Time: {triggerTime}");

            var job = JobBuilder.Create(JobType)
                .WithIdentity(JobName, JobGroup)
                .Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity(TriggerName, TriggerGroup)
                .WithCronSchedule(triggerTime)
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            return scheduler;

        }
    }
}
