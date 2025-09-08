using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsDeptBudgetDtV
{
    public string Setid { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public decimal FiscalYear { get; set; }

    public DateTime BudgetBeginDt { get; set; }

    public DateTime BudgetEndDt { get; set; }

    public string BudgetCapIndc { get; set; } = null!;

    public string DeptOffsetGrp { get; set; } = null!;

    public string DefaultFundOptn { get; set; } = null!;

    public string AcctCdDed { get; set; } = null!;

    public string HpDfltFndDtFlg { get; set; } = null!;

    public DateTime? FundEndDtDed { get; set; }

    public string AcctCdTax { get; set; } = null!;

    public DateTime? FundEndDtTax { get; set; }

    public string HpFringeGroup { get; set; } = null!;

    public string HpErnAcct { get; set; } = null!;

    public string HpDedAcct { get; set; } = null!;

    public string HpTaxAcct { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
