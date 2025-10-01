using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UnionDV
{
    public decimal UnionDKey { get; set; }

    public string UnionUnionCd { get; set; } = null!;

    public DateTime UnionEffDt { get; set; }

    public string UnionEffStatCd { get; set; } = null!;

    public string UnionDesc { get; set; } = null!;

    public string UnionShortDesc { get; set; } = null!;

    public string UnionUnionLocalFlg { get; set; } = null!;

    public string UnionBargUnitCd { get; set; } = null!;

    public string UnionCntryCd { get; set; } = null!;

    public string UnionAddr1Txt { get; set; } = null!;

    public string UnionAddr2Txt { get; set; } = null!;

    public string UnionAddr3Txt { get; set; } = null!;

    public string UnionAddr4Txt { get; set; } = null!;

    public string UnionCityNm { get; set; } = null!;

    public string UnionHouseTypeCd { get; set; } = null!;

    public string UnionCntyNm { get; set; } = null!;

    public string UnionStCd { get; set; } = null!;

    public string UnionPstlCd { get; set; } = null!;

    public string UnionGeoCd { get; set; } = null!;

    public string UnionCntctNm { get; set; } = null!;

    public string UnionPhCntryCd { get; set; } = null!;

    public string UnionCntctPhNum { get; set; } = null!;

    public string UnionStewardNm { get; set; } = null!;

    public DateTime UnionCntrctBegDt { get; set; }

    public DateTime UnionCntrctEndDt { get; set; }

    public string UnionVacationPlanCd { get; set; } = null!;

    public string UnionSickLeavePlanCd { get; set; } = null!;

    public decimal UnionSdiPct { get; set; }

    public string UnionDsbltyInsrncFlg { get; set; } = null!;

    public string UnionLifeInsrncAvlbltyCd { get; set; } = null!;

    public decimal UnionRetrmntPickupPct { get; set; }

    public string UnionFicaPickupFlg { get; set; } = null!;

    public decimal UnionCallbackMinHrsQty { get; set; }

    public decimal UnionCallbackRt { get; set; }

    public string UnionCertfdFlg { get; set; } = null!;

    public string UnionClosedShopFlg { get; set; } = null!;

    public decimal UnionStdHrsQty { get; set; }

    public decimal UnionWrkDayHrsQty { get; set; }

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? UnionSvmSeqNum { get; set; }

    public double? UnionSvmSeqMrf { get; set; }

    public string? UnionSvmIsMostrecent { get; set; }
}
