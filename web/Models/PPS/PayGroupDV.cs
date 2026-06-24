using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PayGroupDV
{
    public decimal PayGrpDKey { get; set; }

    public string PayGrpCmpyCd { get; set; } = null!;

    public string PayGrpCd { get; set; } = null!;

    public DateTime PayGrpEffDt { get; set; }

    public string PayGrpEffStatCd { get; set; } = null!;

    public string PayGrpDesc { get; set; } = null!;

    public string PayGrpPayFreqCd { get; set; } = null!;

    public string PayGrpPayFreqDesc { get; set; } = null!;

    public string PayGrpRetireePaygrpFlg { get; set; } = null!;

    public string PayGrpSrcBankId { get; set; } = null!;

    public string PayGrpEmpTypeCd { get; set; } = null!;

    public string PayGrpEmpTypeDesc { get; set; } = null!;

    public decimal PayGrpNetPayMinAmt { get; set; }

    public decimal PayGrpNetPayMaxAmt { get; set; }

    public string PayGrpPayRptSeqNum { get; set; } = null!;

    public string PayGrpPayRptSeqDesc { get; set; } = null!;

    public string PayGrpPayRptSubtotalCd { get; set; } = null!;

    public string PayGrpPayRptSubtotalDesc { get; set; } = null!;

    public string PayGrpPayRptEmpSeqNum { get; set; } = null!;

    public string PayGrpPayRptEmpSeqDesc { get; set; } = null!;

    public string PayGrpPyckSeqNum { get; set; } = null!;

    public string PayGrpPyckSeqDesc { get; set; } = null!;

    public string PayGrpPyckAddrOptnInd { get; set; } = null!;

    public string PayGrpPyckAddrOptnDesc { get; set; } = null!;

    public string PayGrpPyckLocOptnInd { get; set; } = null!;

    public string PayGrpPyckLocOptnDesc { get; set; } = null!;

    public string PayGrpPyckEmpSeqNum { get; set; } = null!;

    public string PayGrpPyckEmpSeqDesc { get; set; } = null!;

    public string PayGrpPyckFld01SeqNum { get; set; } = null!;

    public string PayGrpPyckFld02SeqNum { get; set; } = null!;

    public string PayGrpPyckFld03SeqNum { get; set; } = null!;

    public string PayGrpPyckFld04SeqNum { get; set; } = null!;

    public string PayGrpPyckFld05SeqNum { get; set; } = null!;

    public string PayGrpPyckFld06SeqNum { get; set; } = null!;

    public string PayGrpPyckFld07SeqNum { get; set; } = null!;

    public string PayGrpPyckFld08SeqNum { get; set; } = null!;

    public string PayGrpPyckFld09SeqNum { get; set; } = null!;

    public string PayGrpPyckFld10SeqNum { get; set; } = null!;

    public decimal PayGrpOasdiErngsBreakAmt { get; set; }

    public decimal PayGrpOasdiLowFactorRt { get; set; }

    public decimal PayGrpOasdiHighExmptRt { get; set; }

    public string PayGrpProrateSlyrdCd { get; set; } = null!;

    public string PayGrpProrateSlyrdDesc { get; set; } = null!;

    public string PayGrpProrateHrlyCd { get; set; } = null!;

    public string PayGrpProrateHrlyDesc { get; set; } = null!;

    public string PayGrpAutoPaysheetUpdtFlg { get; set; } = null!;

    public string PayGrpWrkScheduleInd { get; set; } = null!;

    public string PayGrpDfltBnpgmCd { get; set; } = null!;

    public string PayGrpProgErngsCd { get; set; } = null!;

    public string PayGrpRglrHrsErngsCd { get; set; } = null!;

    public string PayGrpOtHrsErngsCd { get; set; } = null!;

    public string PayGrpRglrErngsCd { get; set; } = null!;

    public string PayGrpHldyErngsCd { get; set; } = null!;

    public string PayGrpHldyScheduleCd { get; set; } = null!;

    public string PayGrpRetropayProgId { get; set; } = null!;

    public string PayGrpRetropayTrgtProgId { get; set; } = null!;

    public string PayGrpFlsaReqrdFlg { get; set; } = null!;

    public string PayGrpFlsaReqrdDesc { get; set; } = null!;

    public string PayGrpDfltSetidCd { get; set; } = null!;

    public string PayGrpDfltSetidDesc { get; set; } = null!;

    public string PayGrpCnfrmErrCd { get; set; } = null!;

    public string PayGrpErrPedOptnCd { get; set; } = null!;

    public string PayGrpErrPedOptnDesc { get; set; } = null!;

    public string PayGrpRtTypeCd { get; set; } = null!;

    public string PayGrpRtCnvrsnDtCd { get; set; } = null!;

    public string PayGrpRtCnvrsnDtDesc { get; set; } = null!;

    public string PayGrpTipsDelayWitheldFlg { get; set; } = null!;

    public string PayGrpTipsAdjstFlg { get; set; } = null!;

    public string PayGrpTipsAdjstErngsCd { get; set; } = null!;

    public string PayGrpTipsCredErngsCd { get; set; } = null!;

    public string PayGrpFlsaSlryHrUsedCd { get; set; } = null!;

    public string PayGrpFlsaSlryHrUsedDesc { get; set; } = null!;

    public string PayGrpTermProgId { get; set; } = null!;

    public string PayGrpWageLossPlanCd { get; set; } = null!;

    public string PayGrp1042ErngsCd { get; set; } = null!;

    public string PayGrpStdntFinPaygrpFlg { get; set; } = null!;

    public string PayGrpIndstrySectorCd { get; set; } = null!;

    public string PayGrpIndstrySectorDesc { get; set; } = null!;

    public string PayGrpIndstryCd { get; set; } = null!;

    public string PayGrpIndstryDesc { get; set; } = null!;

    public string PayGrpFlsaPerTypeCd { get; set; } = null!;

    public string PayGrpFlsaCalId { get; set; } = null!;

    public string PayGrpFlsaBasicFrmlaCd { get; set; } = null!;

    public string PayGrpFlsaBasicFrmlaDesc { get; set; } = null!;

    public string PayGrpFreqMthlyId { get; set; } = null!;

    public string PayGrpFreqDailyId { get; set; } = null!;

    public decimal PayGrpDedctnPrtyNum { get; set; }

    public string PayGrpDdSrcBankId { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
