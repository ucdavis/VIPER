using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobCodeDV
{
    public decimal JobCdDKey { get; set; }

    public string JobCdSetId { get; set; } = null!;

    public string JobCd { get; set; } = null!;

    public DateTime JobCdEffDt { get; set; }

    public string JobCdEffStatCd { get; set; } = null!;

    public string JobCdDesc { get; set; } = null!;

    public string JobCdShortDesc { get; set; } = null!;

    public string JobCdJobFuncCd { get; set; } = null!;

    public string JobCdJobFuncDesc { get; set; } = null!;

    public string JobCdSlrySetId { get; set; } = null!;

    public string JobCdSlryAdminPlanCd { get; set; } = null!;

    public string JobCdSlryAdminPlanDesc { get; set; } = null!;

    public string JobCdGrdCd { get; set; } = null!;

    public string JobCdGrdDesc { get; set; } = null!;

    public decimal JobCdStepCd { get; set; }

    public string JobCdStepDesc { get; set; } = null!;

    public string JobCdUnionCd { get; set; } = null!;

    public string JobCdUnionDesc { get; set; } = null!;

    public string JobCdCurrencyCd { get; set; } = null!;

    public decimal JobCdStdHrsQty { get; set; }

    public string JobCdStdHrsFreqCd { get; set; } = null!;

    public string JobCdStdHrsFreqDesc { get; set; } = null!;

    public string JobCdCompFreqCd { get; set; } = null!;

    public string JobCdCompFreqDesc { get; set; } = null!;

    public string JobCdWrkCompCd { get; set; } = null!;

    public string JobCdJobFmlyCd { get; set; } = null!;

    public string JobCdRglrTempCd { get; set; } = null!;

    public string JobCdRglrTempDesc { get; set; } = null!;

    public string JobCdFlsaStatCd { get; set; } = null!;

    public string JobCdFlsaStatDesc { get; set; } = null!;

    public string JobCdEe01Cd { get; set; } = null!;

    public string JobCdEe01Desc { get; set; } = null!;

    public string JobCdEeoJobGrpCd { get; set; } = null!;

    public string JobCdUsStdOcuptnlCd { get; set; } = null!;

    public string JobCdUsStdOcuptnlDesc { get; set; } = null!;

    public string JobCdUsOcuptnlClsfctnCd { get; set; } = null!;

    public string JobCdUsOcuptnlClsfctnDesc { get; set; } = null!;

    public string JobCdTrngProgCd { get; set; } = null!;

    public string JobCdTrngProgDesc { get; set; } = null!;

    public string JobCdCmpyCd { get; set; } = null!;

    public string JobCdCmpyDesc { get; set; } = null!;

    public string JobCdBargUnitCd { get; set; } = null!;

    public string JobCdBargUnitDesc { get; set; } = null!;

    public string JobCdPosnMgmtCd { get; set; } = null!;

    public string JobCdEduGovAcdmcRankCd { get; set; } = null!;

    public DateTime JobCdLastUpdtDt { get; set; }

    public string JobCdRgltryRegnCd { get; set; } = null!;

    public string JobCdRgltryRegnDesc { get; set; } = null!;

    public string JobCdOcuptnlSubgrpCd { get; set; } = null!;

    public string JobCdAcdmcCompSubgrpCd { get; set; } = null!;

    public string JobCdOcuptnlSubgrpDesc { get; set; } = null!;

    public string JobCdAcdmcCompSubgrpDesc { get; set; } = null!;

    public string JobCdRetrmntSafetyFlg { get; set; } = null!;

    public string JobCdSwLocalCd { get; set; } = null!;

    public string JobCdSwLocalDesc { get; set; } = null!;

    public string JobCdByAgrmtFlg { get; set; } = null!;

    public string JobCdElgblOncallFlg { get; set; } = null!;

    public string JobCdElgblShiftDiffFlg { get; set; } = null!;

    public string JobCdOffscaleFlg { get; set; } = null!;

    public string JobCdElgblSumrSlryFlg { get; set; } = null!;

    public string JobCdUcrpExclsnElgbltyFlg { get; set; } = null!;

    public string JobCdSlryGrdMinRtAnnl { get; set; } = null!;

    public decimal JobCdSlryGrdMidRtAnnl { get; set; }

    public decimal JobCdSlryGrdMaxRtAnnl { get; set; }

    public string JobCdJobFmlyDesc { get; set; } = null!;

    public string JobCdRetrmntSafetyDesc { get; set; } = null!;

    public string JobCdClassCd { get; set; } = null!;

    public string JobCdClassDesc { get; set; } = null!;

    public string JobCdUcFacltyFlg { get; set; } = null!;

    public string JobCdEmpClassCd { get; set; } = null!;

    public string JobCdEmpClassDesc { get; set; } = null!;

    public string JobCdEduGovAcdmcRankDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? JobCdSvmSeqNum { get; set; }

    public double? JobCdSvmSeqMrf { get; set; }

    public string? JobCdSvmIsMostrecent { get; set; }
}
