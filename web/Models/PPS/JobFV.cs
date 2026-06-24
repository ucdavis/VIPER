using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobFV
{
    public DateTime EffDt { get; set; }

    public DateTime ExpctdEndDate { get; set; }

    public DateTime JobActnEffDt { get; set; }

    public decimal EmpDKey { get; set; }

    public decimal JobFEmpRecNum { get; set; }

    public decimal? EffDtKey { get; set; }

    public decimal JobFEffSeqNum { get; set; }

    public decimal JobFKey { get; set; }

    public decimal JobCdDKey { get; set; }

    public decimal JobCdDCurKey { get; set; }

    public decimal JobActnDKey { get; set; }

    public decimal EmpDSupvsrKey { get; set; }

    public decimal EmpDCurKey { get; set; }

    public decimal PosnDKey { get; set; }

    public decimal PosnDCurKey { get; set; }

    public decimal DeptDKey { get; set; }

    public decimal PrmyJobsDKey { get; set; }

    public decimal PayGrpDKey { get; set; }

    public decimal PayGrpDCurKey { get; set; }

    public decimal JobDKey { get; set; }

    public decimal JobStatDEmpKey { get; set; }

    public decimal JobStatDHrKey { get; set; }

    public decimal JobStatDBenKey { get; set; }

    public decimal UcEmpRevwDKey { get; set; }

    public decimal BusUnitDKey { get; set; }

    public decimal UnionDKey { get; set; }

    public decimal OrgDJobKey { get; set; }

    public decimal OrgDHmCurKey { get; set; }

    public decimal OrgDHmKey { get; set; }

    public decimal OrgDJobCurKey { get; set; }

    public decimal JobActnDtKey { get; set; }

    public decimal JobEntryDtKey { get; set; }

    public decimal DeptEntryDtKey { get; set; }

    public decimal PosnEntryDtKey { get; set; }

    public decimal GrdEntryDtKey { get; set; }

    public decimal StepEntryDtKey { get; set; }

    public decimal TermntnDtKey { get; set; }

    public decimal AsgnmtStartDtKey { get; set; }

    public decimal LastAsgnmtStartDtKey { get; set; }

    public decimal AsgnmtEndDtKey { get; set; }

    public decimal LastDayWrkedDtKey { get; set; }

    public decimal ExpctdRtrnDtKey { get; set; }

    public decimal? ExpctdEndDtKey { get; set; }

    public decimal HireDtKey { get; set; }

    public decimal LastHireDtKey { get; set; }

    public decimal TrialEmplymtEndDtKey { get; set; }

    public decimal PrbtnEndDtKey { get; set; }

    public decimal JobFStdHrsQty { get; set; }

    public decimal JobFCompRt { get; set; }

    public decimal JobFChngAmt { get; set; }

    public decimal JobFChngPct { get; set; }

    public decimal JobFAnnlRt { get; set; }

    public decimal JobFMthlyRt { get; set; }

    public decimal JobFDailyRt { get; set; }

    public decimal JobFHrlyRt { get; set; }

    public decimal JobFAnnlBenBaseRt { get; set; }

    public decimal JobFShiftRt { get; set; }

    public decimal JobFShiftFactorPct { get; set; }

    public decimal JobFFtePct { get; set; }

    public decimal JobFWrkDayHrsQty { get; set; }

    public decimal JobFJobCnt { get; set; }

    public decimal UcEmpRedTimeEndDtKey { get; set; }

    public decimal UcLocUseEndDtKey { get; set; }

    public decimal UcPostDoctAnvsDtKey { get; set; }

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? JobFSvmSeqNum { get; set; }

    public double? JobFSvmSeqMrf { get; set; }

    public string? JobFSvmIsMostrecent { get; set; }

    public string? JobFSvmPrimaryIdx { get; set; }

    public string? PosnNum { get; set; }

    public string? JobCode { get; set; }

    public string? JobCodeDesc { get; set; }

    public string? JobCodeShortDesc { get; set; }

    public string? JobGroup { get; set; }

    public string? JobGroupDesc { get; set; }

    public string? JobCodeFlsaStat { get; set; }

    public string? JobCodeUnionDesc { get; set; }

    public string? JobStatusCode { get; set; }

    public string? JobStatusDesc { get; set; }

    public string? DeptCode { get; set; }

    public string? DeptTitle { get; set; }

    public string? DeptShortTitle { get; set; }

    public string? SubDivCode { get; set; }

    public string? SubDivTitle { get; set; }
}
