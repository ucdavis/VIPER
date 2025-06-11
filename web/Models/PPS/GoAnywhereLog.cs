using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class GoAnywhereLog
{
    public int RecordId { get; set; }

    public DateTime Timestamp { get; set; }

    public string User { get; set; } = null!;

    public bool Success { get; set; }

    public string? FileName { get; set; }

    public string? Action { get; set; }

    public string? Message { get; set; }
}
