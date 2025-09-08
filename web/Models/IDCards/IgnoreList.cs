using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class IgnoreList
{
    public int IgnoreId { get; set; }

    public string IamId { get; set; } = null!;

    public bool DoNotGrant { get; set; }

    public bool DoNotRevoke { get; set; }

    public DateTime ModTime { get; set; }

    public string ModBy { get; set; } = null!;
}
