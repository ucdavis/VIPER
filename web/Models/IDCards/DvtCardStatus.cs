using System;
using System.Collections.Generic;

namespace Viper.Models.IDCards;

public partial class DvtCardStatus
{
    /// <summary>
    /// Card Status Code
    /// </summary>
    public string DvtStatusCode { get; set; } = null!;

    /// <summary>
    /// Description of Card Status
    /// </summary>
    public string DvtStatusDesc { get; set; } = null!;

    public string DvtStatusVoidable { get; set; } = null!;

    public string DvtStatusDupOk { get; set; } = null!;

    public string? DvtStatusOverrideOk { get; set; }
}
