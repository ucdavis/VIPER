using Quartz;

namespace Viper.Areas.Jobs.Model
{
    public class JobInfo
    {
        public string JobKey { get; set; } = null!;
        public string JobGroup { get; set; } = null!;
        public string JobClassName { get; set; } = null!;
        public TriggerState? JobState { get; set; }
        public DateTimeOffset? JobStartTime { get; set; }
        public DateTimeOffset? NextRunTime { get; set; }
        public string? JobDescription { get; set; }
        public string? CronExpression { get; set; }
        public string? TriggerDescription { get; set; }
        public string? TimingDescription { get; set; }
        public JobDataMap? Parameters { get; set; }
    }
}
