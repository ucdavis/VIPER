using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsAcctCdTblV
{
    public string FdmHash { get; set; } = null!;

    public string AcctCd { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Account { get; set; } = null!;

    public string DeptidCf { get; set; } = null!;

    public string ProjectId { get; set; } = null!;

    public string Product { get; set; } = null!;

    public string FundCode { get; set; } = null!;

    public string ProgramCode { get; set; } = null!;

    public string ClassFld { get; set; } = null!;

    public string Affiliate { get; set; } = null!;

    public string OperatingUnit { get; set; } = null!;

    public string Altacct { get; set; } = null!;

    public string BudgetRef { get; set; } = null!;

    public string Chartfield1 { get; set; } = null!;

    public string Chartfield2 { get; set; } = null!;

    public string Chartfield3 { get; set; } = null!;

    public string AffiliateIntra1 { get; set; } = null!;

    public string AffiliateIntra2 { get; set; } = null!;

    public string BusinessUnitPc { get; set; } = null!;

    public string ActivityId { get; set; } = null!;

    public string ResourceType { get; set; } = null!;

    public string ResourceCategory { get; set; } = null!;

    public string ResourceSubCat { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string DirectCharge { get; set; } = null!;

    public string EncumbAccount { get; set; } = null!;

    public string PreEncumbAccount { get; set; } = null!;

    public string ProrateLiability { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
