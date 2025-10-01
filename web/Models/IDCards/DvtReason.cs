using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class DvtReason
{
    /// <summary>
    /// Card Status Code
    /// </summary>
    public string DvtReasonCode { get; set; } = null!;

    /// <summary>
    /// Description of Card Status
    /// </summary>
    public string DvtReasonDesc { get; set; } = null!;

    public string DvtReasonVoidable { get; set; } = null!;

    public string DvtReasonDupOk { get; set; } = null!;

    public string? DvtReasonOverrideOk { get; set; }

    public bool? DvtReasonInUse { get; set; }
}
