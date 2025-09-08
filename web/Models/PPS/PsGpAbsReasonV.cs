using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsGpAbsReasonV
{
    public string UsedBy { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string AbsTypeOptn { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string AbsenceReason { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
