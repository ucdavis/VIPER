using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwPapercutUser
{
    public string Name { get; set; } = null!;

    public string? LoginId { get; set; }

    public string? Email { get; set; }

    public string Role { get; set; } = null!;
}
