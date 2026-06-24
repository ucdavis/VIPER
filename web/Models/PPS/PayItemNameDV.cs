using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PayItemNameDV
{
    public decimal PinDKey { get; set; }

    public DateTime PinEffDt { get; set; }

    public DateTime PinExprDt { get; set; }

    public string PinEffStatCd { get; set; } = null!;

    public decimal PinNum { get; set; }

    public string PinCd { get; set; } = null!;

    public string PinNm { get; set; } = null!;

    public string PinTypeCd { get; set; } = null!;

    public string PinDesc { get; set; } = null!;

    public string PinFieldFrmtCd { get; set; } = null!;

    public string PinFieldFrmtDesc { get; set; } = null!;

    public string PinDfntnAsofDtOptnCd { get; set; } = null!;

    public string PinDfntnAsofDtOptnDesc { get; set; } = null!;

    public string PinRecalcInd { get; set; } = null!;

    public string PinUsedByCd { get; set; } = null!;

    public string PinUsedByDesc { get; set; } = null!;

    public string PinCntryCd { get; set; } = null!;

    public string PinCntryDesc { get; set; } = null!;

    public string PinCtgyCd { get; set; } = null!;

    public string PinIndstryCd { get; set; } = null!;

    public string PinOwnrCd { get; set; } = null!;

    public string PinOwnrDesc { get; set; } = null!;

    public string PinClassCd { get; set; } = null!;

    public string PinClassDesc { get; set; } = null!;

    public string PinCalOvrdInd { get; set; } = null!;

    public string PinPayEntlmntOvrdInd { get; set; } = null!;

    public string PinPayGrpOvrdInd { get; set; } = null!;

    public string PinPayeeOvrdInd { get; set; } = null!;

    public string PinPostvInputOvrdInd { get; set; } = null!;

    public string PinElmntOvrdInd { get; set; } = null!;

    public string PinSupportElmntOvrdInd { get; set; } = null!;

    public string PinStoreRsltInd { get; set; } = null!;

    public string PinStoreRsltIfZeroInd { get; set; } = null!;

    public string PinStoreRsltIfZeroDesc { get; set; } = null!;

    public decimal PinParentNum { get; set; }

    public string PinCustom1Nm { get; set; } = null!;

    public string PinCustom2Nm { get; set; } = null!;

    public string PinCustom3Nm { get; set; } = null!;

    public string PinCustom4Nm { get; set; } = null!;

    public string PinCustom5Nm { get; set; } = null!;

    public DateTime PinLastUpdtdDttm { get; set; }

    public string PinLastUpdtdUserId { get; set; } = null!;

    public string PinAutoAsgnTypeCd { get; set; } = null!;

    public string PinAutoAsgnTypeDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
