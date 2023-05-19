using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class VwSftFaculty
{
    public string? EmpTermCode { get; set; }

    public string EmpClientid { get; set; } = null!;

    public string PersonLastName { get; set; } = null!;

    public string PersonFirstName { get; set; } = null!;

    public string? HomeDept { get; set; }
}
