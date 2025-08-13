namespace Viper.Models.ClinicalScheduler
{
    public class Status
    {
        public int GradYear { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public int NumWeeks { get; set; }
        public DateTime ExtRequestOpen { get; set; }
        public DateTime ExtRequestDeadline { get; set; }
        public DateTime ExtRequestReopen { get; set; }
        public string SAStreamCrn { get; set; } = string.Empty;
        public string LAStreamCrn { get; set; } = string.Empty;
        public bool DefaultGradYear { get; set; }
        public bool DefaultSelectionYear { get; set; }
        public bool PublishSchedule { get; set; }
    }
}
