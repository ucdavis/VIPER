using System;
using System.Collections.Generic;

namespace Viper.Models.Courses;

public partial class VwPoaPidmName
{
    public string PersonTermCode { get; set; } = null!;

    public string PersonClientid { get; set; } = null!;

    public string? IdsPidm { get; set; }

    public string? PersonDisplayFullName { get; set; }
}
