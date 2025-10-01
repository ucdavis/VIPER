using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsDeptBudgetErnV
{
    public string Setid { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public decimal FiscalYear { get; set; }

    public string PositionPoolId { get; set; } = null!;

    public string SetidJobcode { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public string PositionNbr { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public DateTime Effdt { get; set; }

    public decimal Effseq { get; set; }

    public string Erncd { get; set; } = null!;

    public decimal BudgetSeq { get; set; }

    public string AcctCd { get; set; } = null!;

    public string GlPayType { get; set; } = null!;

    public decimal BudgetAmt { get; set; }

    public string HpExcess { get; set; } = null!;

    public decimal DistPct { get; set; }

    public decimal PercentEffort { get; set; }

    public DateTime? FundingEndDt { get; set; }

    public string HpUsedDistributn { get; set; } = null!;

    public string HpFringeGroup { get; set; } = null!;

    public string HpRedirectAcct { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public decimal UcPercentPay { get; set; }

    public decimal UcPercentEffort { get; set; }

    public string UcCaptype { get; set; } = null!;

    public decimal MonthlyRt { get; set; }

    public decimal UcCaprateFteYr { get; set; }

    public string RequestId { get; set; } = null!;

    public decimal UcCaprate { get; set; }

    public double? PsDeptBudgetErnSvmSeqNum { get; set; }

    public double? PsDeptBudgetErnSvmSeqMrf { get; set; }

    public string? PsDeptBudgetErnSvmIsMostrecent { get; set; }
}
