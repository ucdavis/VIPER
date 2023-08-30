using System;
using System.Collections.Generic;

namespace Viper.Models.VIPER;

public partial class TblVfNotificationLog
{
    /// <summary>
    /// log entry id
    /// </summary>
    public int NotifyLogId { get; set; }

    /// <summary>
    /// notification id
    /// </summary>
    public int? NotifyId { get; set; }

    /// <summary>
    /// notification sent to list
    /// </summary>
    public string? NotifyLogSendTo { get; set; }

    /// <summary>
    /// notification sent date and time
    /// </summary>
    public DateTime? NotifyLogDatetime { get; set; }
}
