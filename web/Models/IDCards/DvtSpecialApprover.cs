using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class DvtSpecialApprover
{
    public int SpecialApproverId { get; set; }

    public short? SpecialApproverClientType { get; set; }

    public string? SpecialApproverMailId { get; set; }

    public string? SpecialApproverName { get; set; }

    public string? SpecialApproverUnitCode { get; set; }
}
