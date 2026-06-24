using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class Loan
{
    public int LoanId { get; set; }

    public string LoanPidm { get; set; } = null!;

    public string LoanTechPidm { get; set; } = null!;

    public DateTime LoanDate { get; set; }

    public int LoanReason { get; set; }

    public string? LoanComments { get; set; }

    public bool LoanExtendedOk { get; set; }

    public DateTime? LoanDueDate { get; set; }

    public string? LoanAuthorization { get; set; }

    public bool LoanExclude { get; set; }

    public DateTime? LoanExcludeDate { get; set; }

    public int? LoanSdpId { get; set; }

    public string? LoanSignature { get; set; }

    public virtual ICollection<Audit> Audits { get; set; } = new List<Audit>();

    public virtual ICollection<EmailSent> EmailSents { get; set; } = new List<EmailSent>();

    public virtual ICollection<LoanItem> LoanItems { get; set; } = new List<LoanItem>();

    public virtual ICollection<LoanNote> LoanNotes { get; set; } = new List<LoanNote>();

    public virtual Reason LoanReasonNavigation { get; set; } = null!;
}
