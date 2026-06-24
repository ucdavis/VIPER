using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class EcoTimeBadgeExclusion
{
    public int ExclusionId { get; set; }

    public string EmployeeId { get; set; } = null!;

    public string IamId { get; set; } = null!;

    public DateTime Entered { get; set; }

    public string EnteredBy { get; set; } = null!;

    public string? Reason { get; set; }

    public bool Active { get; set; }

    public DateTime? Updated { get; set; }

    public string? UpdatedBy { get; set; }
}
