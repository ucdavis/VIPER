using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobDV
{
    public decimal JobDKey { get; set; }

    public string JobDSalAdmnPlanCd { get; set; } = null!;

    public string JobDSalAdmnPlanDesc { get; set; } = null!;

    public string JobDGrdCd { get; set; } = null!;

    public string JobDGrdDesc { get; set; } = null!;

    public decimal JobDStepNum { get; set; }

    public string JobDLocCd { get; set; } = null!;

    public string JobDLocDesc { get; set; } = null!;

    public string JobDTxLocCd { get; set; } = null!;

    public string JobDTxLocDesc { get; set; } = null!;

    public string JobDShiftCd { get; set; } = null!;

    public string JobDShiftDesc { get; set; } = null!;

    public string JobDRglrTempCd { get; set; } = null!;

    public string JobDRglrTempDesc { get; set; } = null!;

    public string JobDFullPartCd { get; set; } = null!;

    public string JobDFullPartDesc { get; set; } = null!;

    public string JobDCmpyCd { get; set; } = null!;

    public string JobDCmpyDesc { get; set; } = null!;

    public string JobDBaseGrpId { get; set; } = null!;

    public string JobDBaseGrpDesc { get; set; } = null!;

    public string JobDEmpTypeCd { get; set; } = null!;

    public string JobDEmpTypeDesc { get; set; } = null!;

    public string JobDHldyScheduleCd { get; set; } = null!;

    public string JobDHldyScheduleDesc { get; set; } = null!;

    public string JobDStdHrsFreqCd { get; set; } = null!;

    public string JobDOffcrCd { get; set; } = null!;

    public string JobDOffcrDesc { get; set; } = null!;

    public string JobDEmpClassCd { get; set; } = null!;

    public string JobDEmpClassDesc { get; set; } = null!;

    public string JobDErngsDstrbtnTypeCd { get; set; } = null!;

    public string JobDErngsDstrbtnTypeDesc { get; set; } = null!;

    public string JobDCompFreqCd { get; set; } = null!;

    public string JobDCompFreqDesc { get; set; } = null!;

    public string JobDRegnCd { get; set; } = null!;

    public string JobDRegnDesc { get; set; } = null!;

    public string JobDDrctlyTippedCd { get; set; } = null!;

    public string JobDDrctlyTippedDesc { get; set; } = null!;

    public string JobDFlsaStatCd { get; set; } = null!;

    public string JobDFlsaStatDesc { get; set; } = null!;

    public string JobDEeoClassCd { get; set; } = null!;

    public string JobDEeoClassDesc { get; set; } = null!;

    public string JobDUnionLocalFlg { get; set; } = null!;

    public string JobDCertfdFlg { get; set; } = null!;

    public string JobDBargUnitCd { get; set; } = null!;

    public string JobDBargUnitDesc { get; set; } = null!;

    public string JobDCurrencyRtTypeCd { get; set; } = null!;

    public string JobDClassCd { get; set; } = null!;

    public string JobDClassDesc { get; set; } = null!;

    public string JobDEncmbrncOvrdFlg { get; set; } = null!;

    public string JobDFicaStatEeCd { get; set; } = null!;

    public string JobDFicaStatEeDesc { get; set; } = null!;

    public string JobDProrateCountAmtCd { get; set; } = null!;

    public string JobDProrateCountAmtDesc { get; set; } = null!;

    public string JobDPyrlSysCd { get; set; } = null!;

    public string JobDPyrlSysDesc { get; set; } = null!;

    public string JobDCntrctNum { get; set; } = null!;

    public string JobDJobInd { get; set; } = null!;

    public string JobDJobDesc { get; set; } = null!;

    public string JobDBenSysCd { get; set; } = null!;

    public string JobDBenSysDesc { get; set; } = null!;

    public string JobDRptsToCd { get; set; } = null!;

    public string JobDJobDataSrcCd { get; set; } = null!;

    public string JobDJobDataSrcDesc { get; set; } = null!;

    public string JobDEeoRptngRegnEstbId { get; set; } = null!;

    public string JobDEeoRptngRegnEstbDesc { get; set; } = null!;

    public string JobDSupvsrLvlId { get; set; } = null!;

    public string JobDSupvsrLvlDesc { get; set; } = null!;

    public string JobDAbsSysCd { get; set; } = null!;

    public string JobDAbsSysDesc { get; set; } = null!;

    public string JobDPoiTypeCd { get; set; } = null!;

    public string JobDPoiTypeDesc { get; set; } = null!;

    public string JobDLastDtWrkedOvrdFlg { get; set; } = null!;

    public string JobDAutoJobTermntnEndFlg { get; set; } = null!;

    public string JobDGpPayGrpCd { get; set; } = null!;

    public string JobDGpDfltElgbltyGrpFlg { get; set; } = null!;

    public string JobDGpElgbltyGrpCd { get; set; } = null!;

    public string JobDGpOvrdDfltRtTypeFlg { get; set; } = null!;

    public string JobDGpOvrdDfltAsofDtFlg { get; set; } = null!;

    public string JobDGpUseRtAsofDtCd { get; set; } = null!;

    public string JobDGpUseRtAsofDtDesc { get; set; } = null!;

    public string JobDPersOrgCd { get; set; } = null!;

    public string JobDPosnOvrdFlg { get; set; } = null!;

    public string JobDPosnChngRecFlg { get; set; } = null!;

    public string JobDElgbltyCnfgrtn1Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn2Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn3Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn4Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn5Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn6Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn7Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn8Ind { get; set; } = null!;

    public string JobDElgbltyCnfgrtn9Ind { get; set; } = null!;

    public string JobDLumpSumPayFlg { get; set; } = null!;

    public string JobDPrbtnCd { get; set; } = null!;

    public string JobDAcdmcDurtnApntmtCd { get; set; } = null!;

    public string JobDBasActnCd { get; set; } = null!;

    public string JobDCobraActnCd { get; set; } = null!;

    public decimal JobDSlryGrdMinRtAnnl { get; set; }

    public decimal JobDSlryGrdMidRtAnnl { get; set; }

    public decimal JobDSlryGrdMaxRtAnnl { get; set; }

    public string JobDAcdmcDurtnApntmtDesc { get; set; } = null!;

    public string JobDPrbtnDesc { get; set; } = null!;

    public string JobDPersOrgCdDesc { get; set; } = null!;

    public string JobDUcPyCarDurCd { get; set; } = null!;

    public string JobDUcPyCarDurDesc { get; set; } = null!;

    public string JobDUcPayGrpOvrdCd { get; set; } = null!;

    public string JobDUcPayGrpOvrdDesc { get; set; } = null!;

    public string JobDUcElgblGrpOvrdCd { get; set; } = null!;

    public string JobDUcTermOvrdFlg { get; set; } = null!;

    public string JobDUcPrmyJobOvrdFlg { get; set; } = null!;

    public string JobDUcLocUseTypeCd { get; set; } = null!;

    public string JobDUcLocUseTypeDesc { get; set; } = null!;

    public string JobDUcAltWrkWkCd { get; set; } = null!;

    public string JobDUcAltWrkWkDesc { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
