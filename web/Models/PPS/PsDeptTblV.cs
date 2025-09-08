using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsDeptTblV
{
    public string Setid { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string Company { get; set; } = null!;

    public string SetidLocation { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string TaxLocationCd { get; set; } = null!;

    public string ManagerId { get; set; } = null!;

    public string ManagerPosn { get; set; } = null!;

    public decimal BudgetYrEndDt { get; set; }

    public string BudgetLvl { get; set; } = null!;

    public string GlExpense { get; set; } = null!;

    public string Eeo4Function { get; set; } = null!;

    public string Estabid { get; set; } = null!;

    public string Riskcd { get; set; } = null!;

    public string FteEditIndc { get; set; } = null!;

    public string DeptTenureFlg { get; set; } = null!;

    public string UseBudgets { get; set; } = null!;

    public string UseEncumbrances { get; set; } = null!;

    public string UseDistribution { get; set; } = null!;

    public string BudgetDeptid { get; set; } = null!;

    public string ManagerName { get; set; } = null!;

    public string AccountingOwner { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
