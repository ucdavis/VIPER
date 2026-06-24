using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsCompensationV
{
    public string Emplid { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public DateTime Effdt { get; set; }

    public decimal Effseq { get; set; }

    public decimal CompEffseq { get; set; }

    public string CompRatecd { get; set; } = null!;

    public decimal CompRatePoints { get; set; }

    public decimal Comprate { get; set; }

    public decimal CompPct { get; set; }

    public string CompFrequency { get; set; } = null!;

    public string CurrencyCd { get; set; } = null!;

    public string ManualSw { get; set; } = null!;

    public decimal ConvertComprt { get; set; }

    public string RateCodeGroup { get; set; } = null!;

    public decimal ChangeAmt { get; set; }

    public decimal ChangePct { get; set; }

    public decimal ChangePts { get; set; }

    public string FteIndicator { get; set; } = null!;

    public string CmpSrcInd { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
