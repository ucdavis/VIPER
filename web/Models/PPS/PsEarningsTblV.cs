using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsEarningsTblV
{
    public string Erncd { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public decimal ErnSequence { get; set; }

    public string MaintainBalances { get; set; } = null!;

    public string BudgetEffect { get; set; } = null!;

    public string AllowEmpltype { get; set; } = null!;

    public string PaymentType { get; set; } = null!;

    public decimal HrlyRtMaximum { get; set; }

    public decimal PerunitOvrRt { get; set; }

    public decimal EarnFlatAmt { get; set; }

    public string AddGross { get; set; } = null!;

    public string SubjectFwt { get; set; } = null!;

    public string SubjectFica { get; set; } = null!;

    public string SubjectFut { get; set; } = null!;

    public string SubjectReg { get; set; } = null!;

    public string WithholdFwt { get; set; } = null!;

    public string HrsOnly { get; set; } = null!;

    public string ShiftDiffElig { get; set; } = null!;

    public string TaxGrsCompnt { get; set; } = null!;

    public string SpecCalcRtn { get; set; } = null!;

    public decimal FactorMult { get; set; }

    public decimal FactorRateAdj { get; set; }

    public decimal FactorHrsAdj { get; set; }

    public decimal FactorErnAdj { get; set; }

    public string GlExpense { get; set; } = null!;

    public string SubtractEarns { get; set; } = null!;

    public string DedcdPayback { get; set; } = null!;

    public string TaxMethod { get; set; } = null!;

    public decimal EarnYtdMax { get; set; }

    public string BasedOnType { get; set; } = null!;

    public string BasedOnErncd { get; set; } = null!;

    public string BasedOnAccErncd { get; set; } = null!;

    public string AmtOrHours { get; set; } = null!;

    public string PnaUseSglEmpl { get; set; } = null!;

    public string EligForRetropay { get; set; } = null!;

    public string UsedToPayRetro { get; set; } = null!;

    public string EffectOnFlsa { get; set; } = null!;

    public string FlsaCategory { get; set; } = null!;

    public string RegPayIncluded { get; set; } = null!;

    public string TipsCategory { get; set; } = null!;

    public string AddDe { get; set; } = null!;

    public string IncomeCd1042 { get; set; } = null!;

    public string HrsDistSw { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
