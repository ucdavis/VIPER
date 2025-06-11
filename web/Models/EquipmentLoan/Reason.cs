using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class Reason
{
    public int ReasonId { get; set; }

    public string ReasonReason { get; set; } = null!;

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
