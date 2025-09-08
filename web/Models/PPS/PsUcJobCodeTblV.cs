using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcJobCodeTblV
{
    public string Estabid { get; set; } = null!;

    public string UcJobGroup { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string Jobcode { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? JcgSvmSeqNum { get; set; }

    public double? JcgSvmSeqMrf { get; set; }

    public string? JcgSvmIsMostrecent { get; set; }
}
