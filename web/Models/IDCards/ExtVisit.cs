using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class ExtVisit
{
    public int ExtVisitId { get; set; }

    public int? IdcardId { get; set; }

    public bool AuthVerified { get; set; }

    public string Ip { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string? Action { get; set; }
}
