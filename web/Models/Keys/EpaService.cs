using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class EpaService
{
    public int EpaServiceId { get; set; }

    public int EpaId { get; set; }

    public int ServiceId { get; set; }

    public virtual Epa Epa { get; set; } = null!;
}
