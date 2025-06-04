using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class SecureMediaAudit
{
    public int Id { get; set; }

    public string? Action { get; set; }

    public string? Whoby { get; set; }

    public DateTime? Whotime { get; set; }
}
