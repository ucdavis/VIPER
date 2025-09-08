using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class EarnCodeDV
{
    public decimal EarnCdDKey { get; set; }

    public string EarnCd { get; set; } = null!;

    public DateTime EarnCdEffDt { get; set; }

    public string EarnCdStatCd { get; set; } = null!;

    public string EarnCdDesc { get; set; } = null!;

    public string EarnCdShortDesc { get; set; } = null!;

    public decimal EarnCdSeqNum { get; set; }

    public string EarnCdMaintnBalsFlg { get; set; } = null!;

    public string EarnCdBdgtEffectInd { get; set; } = null!;

    public string EarnCdBdgtEffectDesc { get; set; } = null!;

    public string EarnCdAllowEmpTypeCd { get; set; } = null!;

    public string EarnCdAllowEmpTypeDesc { get; set; } = null!;

    public string EarnCdPymtTypeCd { get; set; } = null!;

    public string EarnCdPymtTypeDesc { get; set; } = null!;

    public decimal EarnCdHrlyRtMaxAmt { get; set; }

    public decimal EarnCdPerunitOvrdRt { get; set; }

    public decimal EarnCdFlatAmt { get; set; }

    public string EarnCdAddGrossFlg { get; set; } = null!;

    public string EarnCdSubjFwtFlg { get; set; } = null!;

    public string EarnCdSubjFicaFlg { get; set; } = null!;

    public string EarnCdSubjFutFlg { get; set; } = null!;

    public string EarnCdSubjRglrTxRtsFlg { get; set; } = null!;

    public string EarnCdFwtFlg { get; set; } = null!;

    public string EarnCdHrsOnlyFlg { get; set; } = null!;

    public string EarnCdShiftDiffElgblFlg { get; set; } = null!;

    public string EarnCdTxGrossCompntCd { get; set; } = null!;

    public string EarnCdSpclCalctnFlg { get; set; } = null!;

    public decimal EarnCdFactorMultRt { get; set; }

    public decimal EarnCdFactorRtAdjstAmt { get; set; }

    public decimal EarnCdFactorHrsAdjstAmt { get; set; }

    public decimal EarnCdFactorEarnAdjstAmt { get; set; }

    public string EarnCdGlExpnsNm { get; set; } = null!;

    public string EarnCdSubtractEarnsFlg { get; set; } = null!;

    public string EarnCdDedctnPybckCd { get; set; } = null!;

    public string EarnCdTxMthdCd { get; set; } = null!;

    public string EarnCdTxMthdDesc { get; set; } = null!;

    public decimal EarnCdEarnYtdMaxAmt { get; set; }

    public string EarnCdOnTypeCd { get; set; } = null!;

    public string EarnCdOnTypeDesc { get; set; } = null!;

    public string EarnCdOnEarnCd { get; set; } = null!;

    public string EarnCdOnAcumltrEarnCd { get; set; } = null!;

    public string EarnCdAmtOrHrsInd { get; set; } = null!;

    public string EarnCdAmtOrHrsDesc { get; set; } = null!;

    public string EarnCdPnaUseSingleEmpFlg { get; set; } = null!;

    public string EarnCdElgblForRetropayFlg { get; set; } = null!;

    public string EarnCdUsedToPayRetroFlg { get; set; } = null!;

    public string EarnCdEffectOnFlsaCd { get; set; } = null!;

    public string EarnCdEffectOnFlsaDesc { get; set; } = null!;

    public string EarnCdFlsaCtgyCd { get; set; } = null!;

    public string EarnCdFlsaCtgyDesc { get; set; } = null!;

    public string EarnCdRglrPayIncldFlg { get; set; } = null!;

    public string EarnCdTipsCtgyCd { get; set; } = null!;

    public string EarnCdTipsCtgyDesc { get; set; } = null!;

    public string EarnCdAddDispsblErngsFlg { get; set; } = null!;

    public string EarnCdInc1042Flg { get; set; } = null!;

    public string EarnCdHrsDstrbtnFlg { get; set; } = null!;

    public string EarnCdInc1042Desc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? EarnCdSvmSeqNum { get; set; }

    public double? EarnCdSvmSeqMrf { get; set; }

    public string? EarnCdSvmIsMostrecent { get; set; }
}
