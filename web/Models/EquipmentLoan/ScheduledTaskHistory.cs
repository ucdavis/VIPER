using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class ScheduledTaskHistory
{
    public int ScheduledTaskHistoryId { get; set; }

    public int ScheduledTaskId { get; set; }

    public DateTime Timestamp { get; set; }

    public bool HasErrors { get; set; }

    public string? Messages { get; set; }

    public string? Errors { get; set; }

    public virtual ScheduledTask ScheduledTask { get; set; } = null!;
}
