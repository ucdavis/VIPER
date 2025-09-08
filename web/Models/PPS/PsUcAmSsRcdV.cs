using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcAmSsRcdV
{
    public string? Deptid { get; set; }

    public string Emplid { get; set; } = null!;

    public DateTime? Asofdate { get; set; }

    public decimal EmplRcd { get; set; }

    public string EligGrp { get; set; } = null!;

    public string DescrDept { get; set; } = null!;

    public string DescrPosition { get; set; } = null!;

    public string DescrJobcode { get; set; } = null!;

    public decimal PayPeriodHrs { get; set; }

    public string ServiceInd { get; set; } = null!;

    public decimal UcAccrFactorV { get; set; }

    public decimal UcPrdEarnedV { get; set; }

    public decimal UcAccrFactorS { get; set; }

    public decimal UcPrdEarnedS { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public decimal UcAccrFactorP { get; set; }

    public decimal UcPrdEarnedP { get; set; }
}
