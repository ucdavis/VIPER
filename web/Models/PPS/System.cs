using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class System
{
    public int SystemId { get; set; }

    public string SystemName { get; set; } = null!;

    public string SystemType { get; set; } = null!;
}
