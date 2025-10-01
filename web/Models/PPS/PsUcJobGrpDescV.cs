using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcJobGrpDescV
{
    public string Estabid { get; set; } = null!;

    public string UcJobGroup { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? UcjobgroupSvmSeqNum { get; set; }

    public double? UcjobgroupSvmSeqMrf { get; set; }

    public string? UcjobgroupSvmIsMostrecent { get; set; }
}
