using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AppControl
{
    public int Id { get; set; }

    public string? FolderName { get; set; }

    public bool? Disable { get; set; }

    public string? MessageHeader { get; set; }

    public string? Message { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredTime { get; set; }
}
