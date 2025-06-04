using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class QrtzLock
{
    public string SchedName { get; set; } = null!;

    public string LockName { get; set; } = null!;
}
