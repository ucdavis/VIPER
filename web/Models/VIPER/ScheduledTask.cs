namespace Viper.Models.VIPER
{
    public class ScheduledTask
    {
        public int ScheduledTaskId { get; set; }
        public string TaskName { get; set; } = null!;
        public string TaskUrl { get; set; } = null!;
        public int FrequencyNum { get; set; }
        public string? FrequencyType { get; set; }
        public int HistoryToKeep { get; set; }
    }
}
