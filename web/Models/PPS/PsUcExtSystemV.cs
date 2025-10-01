using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcExtSystemV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string BusinessUnit { get; set; } = null!;

    public string UcExtSystem { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string UcExtSystemId { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? ExtSystemSvmSeq { get; set; }
}
