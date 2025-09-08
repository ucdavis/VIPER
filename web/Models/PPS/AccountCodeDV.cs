using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AccountCodeDV
{
    public decimal AcctCdDKey { get; set; }

    public string AcctCdFdmHash { get; set; } = null!;

    public string AcctCd { get; set; } = null!;

    public string AcctCdDesc { get; set; } = null!;

    public string AcctCdAcctId { get; set; } = null!;

    public string AcctCdDeptChrtfldId { get; set; } = null!;

    public string AcctCdProjId { get; set; } = null!;

    public string AcctCdProductId { get; set; } = null!;

    public string AcctCdFundCd { get; set; } = null!;

    public string AcctCdProgCd { get; set; } = null!;

    public string AcctCdClassFieldCd { get; set; } = null!;

    public string AcctCdAffiliateId { get; set; } = null!;

    public string AcctCdOpertgUnitCd { get; set; } = null!;

    public string AcctCdAltAcctCd { get; set; } = null!;

    public string AcctCdBdgtRefrncCd { get; set; } = null!;

    public string AcctCdChrtfld1Cd { get; set; } = null!;

    public string AcctCdChrtfld2Cd { get; set; } = null!;

    public string AcctCdChrtfld3Cd { get; set; } = null!;

    public string AcctCdAffiliateIntra1Id { get; set; } = null!;

    public string AcctCdAffiliateIntra2Id { get; set; } = null!;

    public string AcctCdBusUnitProjCostId { get; set; } = null!;

    public string AcctCdActvtyId { get; set; } = null!;

    public string AcctCdResrcTypeCd { get; set; } = null!;

    public string AcctCdResrcCtgyId { get; set; } = null!;

    public string AcctCdResrcSubCtgyId { get; set; } = null!;

    public string AcctCdShortDesc { get; set; } = null!;

    public string AcctCdDirectChrgFlg { get; set; } = null!;

    public string AcctCdEncmbrncAcctId { get; set; } = null!;

    public string AcctCdPreEncmbrncAcctId { get; set; } = null!;

    public string AcctCdProrateLiabilityFlg { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
