using Viper.Classes.SQLContext;
using Viper.Models.VIPER;

namespace Viper.Classes.Utilities
{
    public class ScheduledTaskLog
    {
        private readonly VIPERContext viperContext;
        public ScheduledTaskLog(VIPERContext viperContext)
        {
            this.viperContext = viperContext;
        }

        public void RecordTask(string taskName, string? messages = null, string? errors = null)
        {
            var task = viperContext.ScheduledTasks.FirstOrDefault(t => t.TaskName == taskName);
            if (task != null)
            {
                ScheduledTaskHistory s = new()
                {
                    ScheduledTaskId = task.ScheduledTaskId,
                    Messages = messages,
                    Errors = errors,
                    HasErrors = errors != null,
                    Timestamp = DateTime.Now
                };
                viperContext.Add(s);
                viperContext.SaveChanges();
                CleanHistory(task.ScheduledTaskId, task.HistoryToKeep);
            };

        }

        private void CleanHistory(int taskId, int toKeep)
        {
            if(toKeep > 0)
            {
                var history = viperContext.ScheduledTaskHistories
                    .Where(h => h.ScheduledTaskId == taskId)
                    .OrderByDescending(h => h.Timestamp)
                    .ToList();
                //15 rows, keep 10: keep indexes 0-9, delete indexes 10-14
                for (int i = toKeep; i < history.Count; i++)
                {
                    viperContext.Remove(history[i]);
                }
            }

            viperContext.SaveChanges();
        }

    }
}