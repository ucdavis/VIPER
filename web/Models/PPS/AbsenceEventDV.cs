using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AbsenceEventDV
{
    public decimal AbsEvntDKey { get; set; }

    public string AbsEvntEntrySrcCd { get; set; } = null!;

    public string AbsEvntEntrySrcDesc { get; set; } = null!;

    public string AbsEvntActnPrcsInd { get; set; } = null!;

    public string AbsEvntActnPrcsDesc { get; set; } = null!;

    public string AbsEvntVoidedFlg { get; set; } = null!;

    public string AbsEvntRsnCd { get; set; } = null!;

    public string AbsEvntMgrAprvFlg { get; set; } = null!;

    public string AbsEvntWrkflwStatCd { get; set; } = null!;

    public string AbsEvntCnfgrtn1Cd { get; set; } = null!;

    public string AbsEvntCnfgrtn2Cd { get; set; } = null!;

    public string AbsEvntCnfgrtn3Cd { get; set; } = null!;

    public string AbsEvntCnfgrtn4Cd { get; set; } = null!;

    public string AbsEvntWrkflwStatDesc { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
