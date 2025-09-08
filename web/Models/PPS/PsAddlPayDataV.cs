using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsAddlPayDataV
{
    public string Emplid { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public string Erncd { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public decimal AddlSeq { get; set; }

    public string Deptid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public string PositionNbr { get; set; } = null!;

    public string AcctCd { get; set; } = null!;

    public string GlPayType { get; set; } = null!;

    public string AddlPayShift { get; set; } = null!;

    public decimal OthHrs { get; set; }

    public decimal HourlyRt { get; set; }

    public decimal OthPay { get; set; }

    public string AddlpayReason { get; set; } = null!;

    public decimal Sepchk { get; set; }

    public DateTime? EarningsEndDt { get; set; }

    public decimal GoalAmt { get; set; }

    public decimal GoalBal { get; set; }

    public string OkToPay { get; set; } = null!;

    public string DisableDirDep { get; set; } = null!;

    public string ProrateAddlPay { get; set; } = null!;

    public string ProrateCuiWeeks { get; set; } = null!;

    public string PayPeriod1 { get; set; } = null!;

    public string PayPeriod2 { get; set; } = null!;

    public string PayPeriod3 { get; set; } = null!;

    public string PayPeriod4 { get; set; } = null!;

    public string PayPeriod5 { get; set; } = null!;

    public string State { get; set; } = null!;

    public string Locality { get; set; } = null!;

    public decimal TaxPeriods { get; set; }

    public string TaxMethod { get; set; } = null!;

    public string AddlPayFrequency { get; set; } = null!;

    public string DedTaken { get; set; } = null!;

    public string DedSubsetId { get; set; } = null!;

    public string DedTakenGenl { get; set; } = null!;

    public string DedSubsetGenl { get; set; } = null!;

    public string PlanType { get; set; } = null!;

    public string BusinessUnit { get; set; } = null!;

    public string CompRatecd { get; set; } = null!;

    public string RecordSource { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? PsAddlPaySvmSeqNum { get; set; }

    public double? PsAddlPaySvmSeqMrf { get; set; }

    public string? PsAddlPaySvmIsMostrecent { get; set; }
}
