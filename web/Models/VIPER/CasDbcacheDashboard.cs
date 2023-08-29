using System;
using System.Collections.Generic;

namespace Viper.Models.VIPER;

public partial class CasDbcacheDashboard
{
    public string CookieId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public DateTime TimeStamp { get; set; }

    public DateTime? OriginalTimeStamp { get; set; }
}
