using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class DvtOverseer
{
    public int OverseerId { get; set; }

    public string OverseerRole { get; set; } = null!;

    public string OverseerMailIds { get; set; } = null!;

    public string OverseerSalutation { get; set; } = null!;
}
