using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UpdateLog
{
    public int Id { get; set; }

    public string PortionUpdated { get; set; } = null!;

    public DateTime? Timestamp { get; set; }
}
