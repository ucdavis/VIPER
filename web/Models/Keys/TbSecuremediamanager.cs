using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class TbSecuremediamanager
{
    public int Id { get; set; }

    public string? Action { get; set; }

    public string? ActionItem { get; set; }

    public string? Who { get; set; }

    public DateTime? Time { get; set; }
}
