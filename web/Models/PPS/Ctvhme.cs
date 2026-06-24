using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class Ctvhme
{
    public string? HmeDeptNo { get; set; }

    public string? HmeDeptLocCode { get; set; }

    public string? HmeDeptName { get; set; }

    public string? HmeAbrvDeptName { get; set; }

    public string? HmeDeptAddress { get; set; }

    public string? HmeDeptMailCode { get; set; }

    public string? HmeCampusData { get; set; }

    public string? HmePickupMail { get; set; }

    public string? HmeOnlyAddress { get; set; }

    public string? HmeLastAction { get; set; }

    public DateTime? HmeLastActionDt { get; set; }

    public string? HmeLayoffUnitCd { get; set; }

    public string? HmeOrgUnitCd { get; set; }

    public string? HmeCntlPoint { get; set; }

    public string? UcdLocationCode { get; set; }

    public string? UcdLocationDescription { get; set; }

    public string? UcdSchoolDivision { get; set; }

    public string? UcdDivisionDescription { get; set; }

    public string? SchAbbrv { get; set; }

    public string? UcdDeptName { get; set; }

    public string? UsedInd { get; set; }

    public string? PrimaryInd { get; set; }

    public string? HmeSubLocation { get; set; }

    public string? HmeDeptTypeCd { get; set; }
}
