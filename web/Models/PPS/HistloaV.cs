using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class HistloaV
{
    public string EmployeeId { get; set; } = null!;

    public string? EmpName { get; set; }

    public string? EmpStatus { get; set; }

    public string? HomeDept { get; set; }

    public DateTime? LoaBeginDate { get; set; }

    public DateTime? LoaReturnDate { get; set; }

    public string? LoaTypeCode { get; set; }

    public string? LoaStatusInd { get; set; }

    public string? TitleCode { get; set; }

    public string? ApptType { get; set; }

    public string? PersonalPgmCd { get; set; }

    public string? RepCode { get; set; }

    public string? TitleUnitCode { get; set; }

    public string? UcdSchoolDivision { get; set; }

    public string? HmeDeptName { get; set; }

    public string? Academic { get; set; }
}
