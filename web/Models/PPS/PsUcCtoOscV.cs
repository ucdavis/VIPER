using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcCtoOscV
{
    public string UcCtoOsCd { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr50 { get; set; } = null!;

    public double CrBtNbr { get; set; }

    public DateTime CrBtDtm { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? CtoOscSvmSeqNum { get; set; }

    public double? CtoOscSvmSeqMrf { get; set; }

    public string? CtoOscSvmIsMostrecent { get; set; }
}
