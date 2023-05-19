using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwUConnectUnit
{
    public int? UnitId { get; set; }

    public string? DvtUnitDeptCode { get; set; }

    public string DvtUnitUnitName { get; set; } = null!;

    public string? DeanDirectorMailId { get; set; }

    public string? AdminEmail { get; set; }
}
