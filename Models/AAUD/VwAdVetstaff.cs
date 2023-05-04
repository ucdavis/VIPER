using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwAdVetstaff
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? Loginid { get; set; }

    public string PersonTermCode { get; set; } = null!;

    public string EmpHomeDept { get; set; } = null!;

    public string EmpAltDeptCode { get; set; } = null!;
}
