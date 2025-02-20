using Quartz;
using System.Text.Json;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;

namespace Viper.Areas.Jobs.JobClasses
{
    public class RapsRoleViewUpdate : BaseJob
    {
        readonly RAPSContext rapsContext;

        public RapsRoleViewUpdate(VIPERContext viperContext, RAPSContext rapsContext) : base(viperContext)
        {
            this.rapsContext = rapsContext;
        }

        public override async Task<Task> Execute(IJobExecutionContext context)
        {
            var data = context.MergedJobDataMap;
            logger.Info("RAPS Role View Update Started");
            RoleViews rv = new(rapsContext);
            List<string> messages = await rv.UpdateRoles();
            ScheduledTaskLog s = new(viperContext);
            s.RecordTask("RAPS Views Update", JsonSerializer.Serialize(messages));
            return Task.CompletedTask;
        }
    }
}
