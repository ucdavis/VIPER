using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class DepartmentBudgetEarnFV
{
    public decimal BusUnitDKey { get; set; }

    public decimal DeptDKey { get; set; }

    public decimal FsclYrDtKey { get; set; }

    public decimal PosnPoolDKey { get; set; }

    public decimal JobCdDKey { get; set; }

    public decimal PosnDKey { get; set; }

    public decimal EffDtKey { get; set; }

    public decimal DepbdgernFBdgtEffSeqNum { get; set; }

    public decimal EarnCdDKey { get; set; }

    public decimal DepbdgernFBdgtSeqNum { get; set; }

    public decimal AcctCdDKey { get; set; }

    public decimal DepbdgernFKey { get; set; }

    public decimal DeptBdgtDKey { get; set; }

    public decimal DeptbdgtdtDKey { get; set; }

    public decimal DepbdgernDKey { get; set; }

    public decimal Fau4DKey { get; set; }

    public decimal FausubDKey { get; set; }

    public decimal Fau2DKey { get; set; }

    public decimal? FundingEndDtKey { get; set; }

    public decimal DeptBdgtFKey { get; set; }

    public decimal DepbdgernFBdgtAmt { get; set; }

    public decimal DepbdgernFDstrbtnPct { get; set; }

    public decimal DepbdgernFEffortPct { get; set; }

    public decimal DepbdgernFCnt { get; set; }

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public DateTime DBudgetSvmEffDt { get; set; }

    public DateTime DBudgetSvmEndDt { get; set; }

    public double? DBudgetFSvmSeqNum { get; set; }

    public double? DBudgetFSvmSeqMrf { get; set; }

    public string? DBudgetFSvmIsMostrecent { get; set; }
}
