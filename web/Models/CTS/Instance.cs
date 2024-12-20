﻿namespace Viper.Models.CTS
{
    public class Instance
    {
        public int InstanceId { get; set; }
        public int InstanceEvalId { get; set; }
        public string InstanceMothraId { get; set; } = null!;
        public string InstanceStatus { get; set; } = null!;
        public string? InstanceMode { get; set; }
        public DateTime? InstanceDueDate { get; set; }
        public DateTime? InstanceStartWeek { get; set; }
        public int? InstanceStartWeekId { get; set; }
    }
}
