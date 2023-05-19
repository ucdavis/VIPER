using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwAdTeachingFaculty
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? PersonDisplayMiddleName { get; set; }

    public string? Loginid { get; set; }

    public string PersonTermCode { get; set; } = null!;
}
