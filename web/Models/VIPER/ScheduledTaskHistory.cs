namespace Viper.Models.VIPER
{
    public class ScheduledTaskHistory
    {
        public int ScheduledTaskHistoryId { get; set; }
        public int ScheduledTaskId { get; set; }
        public DateTime Timestamp { get; set; }
        public bool HasErrors { get; set; }
        public string Messages { get; set; } = null!;
        public string Errors { get; set; } = null!;
    }
}
