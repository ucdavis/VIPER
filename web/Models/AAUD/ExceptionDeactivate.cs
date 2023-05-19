using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class ExceptionDeactivate
{
    public string DeactivatePKey { get; set; } = null!;

    public string DeactivateTermCode { get; set; } = null!;

    public string DeactivateClientId { get; set; } = null!;

    public string? DeactivateName { get; set; }

    public DateTime DeactivateDate { get; set; }

    public string? DeactivateBy { get; set; }
}
