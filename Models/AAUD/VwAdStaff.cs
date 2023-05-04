using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwAdStaff
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? Loginid { get; set; }

    public bool FlagsStaff { get; set; }

    public string EmpHomeDept { get; set; } = null!;

    public bool FlagsTeachingFaculty { get; set; }

    public string EmpAltDeptCode { get; set; } = null!;
}
