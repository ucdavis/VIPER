using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class QrtzPausedTriggerGrp
{
    public string SchedName { get; set; } = null!;

    public string TriggerGroup { get; set; } = null!;
}
