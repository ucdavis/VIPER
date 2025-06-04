using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class ScheduledTask
{
    public int ScheduledTaskId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? TaskUrl { get; set; }

    public int? FrequencyNum { get; set; }

    public string? FrequencyType { get; set; }

    public int HistoryToKeep { get; set; }

    public virtual ICollection<ScheduledTaskHistory> ScheduledTaskHistories { get; set; } = new List<ScheduledTaskHistory>();
}
