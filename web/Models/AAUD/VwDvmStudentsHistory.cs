using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwDvmStudentsHistory
{
    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string? Pidm { get; set; }

    public string LatestTermcode { get; set; } = null!;

    public string FirstTermcode { get; set; } = null!;

    public string? LatestClassLevel { get; set; }

    public string? FirstClassLevel { get; set; }

    public int? LatestGradYear { get; set; }

    public int? EnteringGradYear { get; set; }
}
