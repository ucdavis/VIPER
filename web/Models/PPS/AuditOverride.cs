using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AuditOverride
{
    public int Id { get; set; }

    public string Area { get; set; } = null!;

    public string Action { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string? Before { get; set; }

    public string? After { get; set; }
}
