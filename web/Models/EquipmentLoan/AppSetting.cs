using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class AppSetting
{
    public string? LateReportEmails { get; set; }

    public int? LateReportDays { get; set; }
}
