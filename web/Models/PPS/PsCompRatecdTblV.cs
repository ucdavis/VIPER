using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsCompRatecdTblV
{
    public string CompRatecd { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string CompBasePaySw { get; set; } = null!;

    public string UseHighestRtSw { get; set; } = null!;

    public string CompRateType { get; set; } = null!;

    public decimal Comprate { get; set; }

    public decimal CompPct { get; set; }

    public string CompFrequency { get; set; } = null!;

    public string CurrencyCd { get; set; } = null!;

    public string Erncd { get; set; } = null!;

    public string FteIndicator { get; set; } = null!;

    public string RateCodeClass { get; set; } = null!;

    public string SeniorityCalc { get; set; } = null!;

    public string SalPkgWarn { get; set; } = null!;

    public string CmpNonUpdInd { get; set; } = null!;

    public string CmpCalcBy { get; set; } = null!;

    public string CmpPayableSw { get; set; } = null!;

    public string LookupId { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
