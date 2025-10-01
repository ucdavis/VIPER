using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class LoanNote
{
    public int LoannoteId { get; set; }

    public int LoannoteLoanid { get; set; }

    public DateTime LoannoteDate { get; set; }

    public string LoannotePidm { get; set; } = null!;

    public string LoannoteNote { get; set; } = null!;

    public virtual Loan LoannoteLoan { get; set; } = null!;
}
