using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class SessionTimeout
{
    public string Loginid { get; set; } = null!;

    public DateTime SessionTimeout1 { get; set; }

    public string Service { get; set; } = null!;
}
