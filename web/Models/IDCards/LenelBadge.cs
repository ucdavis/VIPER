using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class LenelBadge
{
    public int LenelBadgeId { get; set; }

    public int LenelBadgeKey { get; set; }

    public string IamId { get; set; } = null!;

    public DateTime Imported { get; set; }

    public DateTime? Inactive { get; set; }

    public DateTime? Activate { get; set; }

    public DateTime? Deactivate { get; set; }

    public DateTime? LastChanged { get; set; }

    public int? BadgeType { get; set; }

    public int? Status { get; set; }
}
