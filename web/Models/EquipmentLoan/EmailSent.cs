using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class EmailSent
{
    public int EmailId { get; set; }

    public int EmailLoanid { get; set; }

    public string EmailPidm { get; set; } = null!;

    public string EmailAddress { get; set; } = null!;

    public DateTime EmailSent1 { get; set; }

    public string EmailType { get; set; } = null!;

    public bool EmailManual { get; set; }

    public string? EmailSender { get; set; }

    public string? EmailText { get; set; }

    public virtual Loan EmailLoan { get; set; } = null!;
}
