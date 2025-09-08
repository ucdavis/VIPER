using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class LoanItem
{
    public int LoanitemId { get; set; }

    public int LoanitemLoanid { get; set; }

    public int LoanitemAssetid { get; set; }

    public DateTime LoanitemCheckout { get; set; }

    public string LoanitemCheckoutPidm { get; set; } = null!;

    public DateTime? LoanitemCheckin { get; set; }

    public string? LoanitemCheckinPidm { get; set; }

    public string? LoanitemComment { get; set; }

    public virtual Asset LoanitemAsset { get; set; } = null!;

    public virtual Loan LoanitemLoan { get; set; } = null!;
}
