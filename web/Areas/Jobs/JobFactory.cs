using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using NLog;
using Quartz;
using Quartz.Spi;

namespace Viper.Areas.Jobs
{
    public class JobFactory : IJobFactory
    {
        private readonly NLog.Logger logger;
        private readonly IScheduler scheduler;
        private readonly IServiceProvider serviceProvider;

        public static readonly List<WorkerJob> Workers = new List<WorkerJob>()
        {
            new TestJob()
        };

        public static void AddJobSingletons(IServiceCollection services)
        {
            services.AddSingleton<JobManagerJob>();
            services.AddSingleton<TestJob>();
        }

        public JobFactory(IServiceProvider serviceProvider, IScheduler workerScheduler)
        {
            logger = LogManager.GetCurrentClassLogger();
            scheduler = workerScheduler;
            this.serviceProvider = serviceProvider;
            logger.Info("Job Factory initialized.");
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            IJob? job = null;
            if(bundle.JobDetail.Key.Name == JobManagerJob.JobName)
            {
                var jobManagerJob = serviceProvider.GetService<JobManagerJob>();
                if(jobManagerJob != null)
                {
                    jobManagerJob.AddWorkers(scheduler, Workers);
                    job = jobManagerJob;
                }
            }
            else
            {
                foreach(var w in Workers)
                {
                    if(bundle.JobDetail.Key.Name == w.JobName)
                    {
                        job = w.GetJob(serviceProvider);
                    }
                }
            }
            return job!;
        }

        public void ReturnJob(IJob job)
        {
            var disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
