namespace Viper.Areas.Jobs.Model
{
    public class AddUpdateJob
    {
        public string JobKey { get; set; } = null!;
        public string JobGroup { get; set; } = null!;
        public string JobDescription { get; set; } = null!;
        public string CronExpression { get; set; } = null!;
        public string JobType { get; set; } = null!;
        public string? TimingDescription { get; set; } = null!;
    }
}
