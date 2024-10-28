using NLog;
using Quartz;

namespace Viper.Areas.Jobs
{
    public class JobManagerJob : IJob
    {
        public static string JobName = "JobManagerJob";
        public static string JobGroup = "JobManagerJobGroup";
        public static string TriggerName = "JobManagerTrigger";
        public static string TriggerGroup = "JobManagerTriggerGroup";
        private readonly Logger logger;

        IScheduler? _workerScheduler = null;
        List<WorkerJob> _workers = new List<WorkerJob>();

        public JobManagerJob() : base()
        {
            logger = LogManager.GetCurrentClassLogger();
        }

        public void AddWorkers(IScheduler workerScheduler, List<WorkerJob> workers)
        {
            _workerScheduler = workerScheduler;
            _workers = workers;
        }

        ITrigger GetNewTrigger(string name, string group, string schedule)
        {
            return TriggerBuilder.Create()
                .WithIdentity(name, group)
                .WithCronSchedule(schedule)
                .Build();
        }

        async Task rescheduleJob()
        {
            if (_workerScheduler != null)
            {
                foreach (var worker in _workers)
                {
                    var job = await _workerScheduler.GetJobDetail(worker.JobKey);
                    var trigger = await _workerScheduler.GetTrigger(worker.TriggerKey);
                    if (trigger != null)
                    {
                        var cronTrigger = (ICronTrigger)trigger;
                        var i = new Random().Next(10, 30).ToString();
                        var newExpression = $"0/{i} * * ? * *";

                        if (cronTrigger.CronExpressionString != newExpression)
                        {
                            logger.Info($"Rescheduling job for trigger {trigger.Key.Name} {newExpression}");
                            var newTrigger = GetNewTrigger(trigger.Key.Name, trigger.Key.Group, newExpression);
                            await _workerScheduler.RescheduleJob(trigger.Key, newTrigger);
                        }
                        else
                        {
                            logger.Info("Job does not need to be rescheduled.");
                        }
                    }
                }

            }
            else
            {
                logger.Warn("Could not reschedule worker as the scheduler is null");
            }
        }
        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                var task = Task.Run(async () => await rescheduleJob());
                task.Wait();

                if (_workerScheduler != null && !_workerScheduler.IsStarted)
                {
                    Console.WriteLine("Starting worker scheduler as it has not been started yet");
                    _workerScheduler.Start();
                }

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine("Received an error when executing Zookeeper job. " + e.Message);
                return Task.FromException(e);
            }
        }

    }
}
