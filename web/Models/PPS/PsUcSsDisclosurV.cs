using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcSsDisclosurV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string UnionCd { get; set; } = null!;

    public string UcAddrRel { get; set; } = null!;

    public string UcPhoneRel { get; set; } = null!;

    public string UcMobileRel { get; set; } = null!;

    public string UcEmailRel { get; set; } = null!;

    public string UcAddrProc { get; set; } = null!;

    public string UcPhoneProc { get; set; } = null!;

    public string UcSpouseProc { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string ConfirmAccept { get; set; } = null!;

    public string UcDisclosureStat { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
