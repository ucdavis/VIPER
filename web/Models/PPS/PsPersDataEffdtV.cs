using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsPersDataEffdtV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string Sex { get; set; } = null!;

    public string HighestEducLvl { get; set; } = null!;

    public string FtStudent { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime? UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string RegRegion { get; set; } = null!;

    public string Pronoun { get; set; } = null!;

    public double? PsPersDataSvmDeptSeqNum { get; set; }

    public string? PsPersDataSvmPrimary { get; set; }

    public double? PsPersDataSvmSeqNum { get; set; }

    public double? PsPersDataSvmSeqMrf { get; set; }

    public string? PsPersDataSvmIsMostrecent { get; set; }
}
