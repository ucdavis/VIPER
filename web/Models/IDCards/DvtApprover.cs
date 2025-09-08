using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class DvtApprover
{
    public int DvtApproverId { get; set; }

    public string DvtApproverUnitCode { get; set; } = null!;

    public string? DvtApproverMailId { get; set; }

    public string? DvtApproverName { get; set; }

    public string? DvtApproverUnitName { get; set; }
}
