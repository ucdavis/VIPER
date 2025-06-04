using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class CtsAudit
{
    public int CtsAuditId { get; set; }

    public int ModifiedBy { get; set; }

    public DateTime Timestamp { get; set; }

    public string Area { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? Detail { get; set; }

    public int? EncounterId { get; set; }

    public virtual Encounter? Encounter { get; set; }

    public virtual Person ModifiedByNavigation { get; set; } = null!;
}
