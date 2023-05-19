using System;
using System.Collections.Generic;

namespace Viper.Models.RAPS;

public partial class VwSvmAcademicDeptStaffAndFaculty
{
    public string? MemberId { get; set; }

    public string Last { get; set; } = null!;

    public string First { get; set; } = null!;

    public string IdsTermCode { get; set; } = null!;
}
