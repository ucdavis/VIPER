using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PositionDV
{
    public decimal PosnDKey { get; set; }

    public string PosnNum { get; set; } = null!;

    public DateTime PosnEffDt { get; set; }

    public string PosnEffStatCd { get; set; } = null!;

    public string PosnDesc { get; set; } = null!;

    public string PosnActnCd { get; set; } = null!;

    public string PosnActnDesc { get; set; } = null!;

    public string PosnActnRsnCd { get; set; } = null!;

    public string PosnActnRsnDesc { get; set; } = null!;

    public DateTime PosnActnDt { get; set; }

    public string PosnBusUnitCd { get; set; } = null!;

    public string PosnBusUnitDesc { get; set; } = null!;

    public string PosnDeptCd { get; set; } = null!;

    public string PosnDeptDesc { get; set; } = null!;

    public string PosnJobCd { get; set; } = null!;

    public string PosnJobCdDesc { get; set; } = null!;

    public string PosnStatCd { get; set; } = null!;

    public string PosnStatDesc { get; set; } = null!;

    public DateTime PosnStatDt { get; set; }

    public string PosnBdgtedFlg { get; set; } = null!;

    public string PosnCnfdntlFlg { get; set; } = null!;

    public string PosnKeyFlg { get; set; } = null!;

    public string PosnKeyDesc { get; set; } = null!;

    public string PosnJobShareFlg { get; set; } = null!;

    public string PosnUpdtIncmbntFlg { get; set; } = null!;

    public decimal PosnMaxHeadCountQty { get; set; }

    public string PosnRptsToNm { get; set; } = null!;

    public string PosnRptDottedLineNm { get; set; } = null!;

    public string PosnOrgCd { get; set; } = null!;

    public string PosnOrgInd { get; set; } = null!;

    public string PosnLocCd { get; set; } = null!;

    public string PosnLocDesc { get; set; } = null!;

    public string PosnMailDropCd { get; set; } = null!;

    public string PosnCntryCd { get; set; } = null!;

    public string PosnCntryDesc { get; set; } = null!;

    public string PosnPhNum { get; set; } = null!;

    public string PosnCmpyCd { get; set; } = null!;

    public string PosnCmpyDesc { get; set; } = null!;

    public decimal PosnStdHrsQty { get; set; }

    public string PosnStdHrsFreqCd { get; set; } = null!;

    public string PosnStdHrsFreqDesc { get; set; } = null!;

    public string PosnUnionCd { get; set; } = null!;

    public string PosnUnionDesc { get; set; } = null!;

    public string PosnShiftInd { get; set; } = null!;

    public string PosnShiftDesc { get; set; } = null!;

    public string PosnRglrTempInd { get; set; } = null!;

    public string PosnRglrTempDesc { get; set; } = null!;

    public string PosnFullPartInd { get; set; } = null!;

    public string PosnFullPartDesc { get; set; } = null!;

    public decimal PosnHrsMonQty { get; set; }

    public decimal PosnHrsTueQty { get; set; }

    public decimal PosnHrsWedQty { get; set; }

    public decimal PosnHrsThuQty { get; set; }

    public decimal PosnHrsFriQty { get; set; }

    public decimal PosnHrsSatQty { get; set; }

    public decimal PosnHrsSunQty { get; set; }

    public string PosnBargUnitCd { get; set; } = null!;

    public string PosnBargUnitDesc { get; set; } = null!;

    public string PosnSeasonlInd { get; set; } = null!;

    public string PosnTrngProgCd { get; set; } = null!;

    public string PosnTrngProgDesc { get; set; } = null!;

    public string PosnLangSkillCd { get; set; } = null!;

    public string PosnLangSkillDesc { get; set; } = null!;

    public string PosnMgrLvlCd { get; set; } = null!;

    public string PosnMgrLvlDesc { get; set; } = null!;

    public string PosnFlsaStatCd { get; set; } = null!;

    public string PosnFlsaStatDesc { get; set; } = null!;

    public string PosnRgltryRegnCd { get; set; } = null!;

    public string PosnRgltryRegnDesc { get; set; } = null!;

    public string PosnClassInd { get; set; } = null!;

    public string PosnClassDesc { get; set; } = null!;

    public string PosnEncmbrncInd { get; set; } = null!;

    public string PosnEncmbrncDesc { get; set; } = null!;

    public decimal PosnFtePct { get; set; }

    public string PosnPoolId { get; set; } = null!;

    public string PosnPoolIdDesc { get; set; } = null!;

    public string PosnEduGovAcdmcRankInd { get; set; } = null!;

    public string PosnEduGovGrpInd { get; set; } = null!;

    public string PosnEncmbrSlryOptnCd { get; set; } = null!;

    public string PosnEncmbrSlryOptnDesc { get; set; } = null!;

    public decimal PosnEncmbrSlryAmt { get; set; }

    public string PosnHlthCertFlg { get; set; } = null!;

    public string PosnHlthCertDesc { get; set; } = null!;

    public string PosnSgntrAuthtyFlg { get; set; } = null!;

    public string PosnSgntrAuthtyDesc { get; set; } = null!;

    public string PosnAddsToFteActlFlg { get; set; } = null!;

    public string PosnSlryAdminPlanCd { get; set; } = null!;

    public string PosnSlryAdminPlanDesc { get; set; } = null!;

    public string PosnGrdCd { get; set; } = null!;

    public string PosnGrdCdDesc { get; set; } = null!;

    public decimal PosnStepNum { get; set; }

    public string PosnSupvsryLvlCd { get; set; } = null!;

    public string PosnSupvsryLvlDesc { get; set; } = null!;

    public string PosnIncldSlryPlanFlg { get; set; } = null!;

    public DateTime PosnLastUpdtDt { get; set; }

    public string PosnLastUpdtLogonId { get; set; } = null!;

    public string PosnScrtyClrncTypeCd { get; set; } = null!;

    public string PosnScrtyClrncTypeDesc { get; set; } = null!;

    public string PosnAvlblTelewrkPosnFlg { get; set; } = null!;

    public string PosnEmpRltnshpCd { get; set; } = null!;

    public string PosnSpclTrngCd { get; set; } = null!;

    public string PosnRepCd { get; set; } = null!;

    public string PosnUcFteJobPosnSameFlg { get; set; } = null!;

    public string PosnEmpRltnshpDesc { get; set; } = null!;

    public string PosnRepDesc { get; set; } = null!;

    public string PosnSrcId { get; set; } = null!;

    public string PosnUcHrGrp { get; set; } = null!;

    public string PosnEduGovAcdmcRankDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? PosnSvmSeqNum { get; set; }

    public double? PosnSvmSeqMrf { get; set; }

    public string? PosnSvmIsMostrecent { get; set; }
}
