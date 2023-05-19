using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwAdVmssacscheduler
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? Loginid { get; set; }

    public string? Email { get; set; }

    public string? IdCardLine2 { get; set; }

    public string EmpHomeDept { get; set; } = null!;

    public string EmpAltDeptCode { get; set; } = null!;
}
