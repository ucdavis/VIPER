using System;
using System.Collections.Generic;

namespace Viper.Models.AAUD;

public partial class Exceptionemployee
{
    public string EmpPKey { get; set; } = null!;

    public string EmpTermCode { get; set; } = null!;

    public string EmpClientid { get; set; } = null!;

    public string EmpHomeDept { get; set; } = null!;

    public string EmpAltDeptCode { get; set; } = null!;

    public string EmpSchoolDivision { get; set; } = null!;

    public string EmpCbuc { get; set; } = null!;

    public string EmpStatus { get; set; } = null!;

    public string EmpPrimaryTitle { get; set; } = null!;

    public string? EmpTeachingTitleCode { get; set; }

    public string? EmpTeachingHomeDept { get; set; }

    public decimal? EmpTeachingPercentFulltime { get; set; }

    public string? EmpEffortTitleCode { get; set; }

    public string? EmpEffortHomeDept { get; set; }
}
