using NLog;
using Quartz;
using Viper.Classes.SQLContext;

namespace Viper.Areas.Jobs.JobClasses
{
    public class BaseJob : IJob
    {
        protected readonly VIPERContext viperContext;
        protected readonly Logger logger;
        public BaseJob(VIPERContext viperContext)
        {
            this.viperContext = viperContext;
            logger = LogManager.GetCurrentClassLogger();
        }

        public virtual Task Execute(IJobExecutionContext context)
        {
            logger.Info("Base job completed");
            return Task.CompletedTask;
        }
    }
}
