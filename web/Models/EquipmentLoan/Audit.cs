using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class Audit
{
    public int AuditId { get; set; }

    public int AuditCode { get; set; }

    public string AuditTypeDesc { get; set; } = null!;

    public DateTime AuditTimestamp { get; set; }

    public int? AuditLoan { get; set; }

    public string? AuditUserPidm { get; set; }

    public string? AuditTechPidm { get; set; }

    public string? AuditAdditionalInfo { get; set; }

    public virtual Loan? AuditLoanNavigation { get; set; }
}
