using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AbsenceResultDV
{
    public decimal AbsRsltDKey { get; set; }

    public string AbsRsltCntryCd { get; set; } = null!;

    public string AbsRsltCntryDesc { get; set; } = null!;

    public string AbsRsltAcumTypeCd { get; set; } = null!;

    public string AbsRsltAcumTypeDesc { get; set; } = null!;

    public string AbsRsltAcumPerOptnCd { get; set; } = null!;

    public string AbsRsltAcumPerOptnDesc { get; set; } = null!;

    public string AbsRsltCrctvRetroMthdFlg { get; set; } = null!;

    public string AbsRsltValdInSgmntFlg { get; set; } = null!;

    public string AbsRsltCalledInSgmntFlg { get; set; } = null!;

    public string AbsRsltSubUserAcumltr1Nm { get; set; } = null!;

    public string AbsRsltSubUserAcumltr2Nm { get; set; } = null!;

    public string AbsRsltSubUserAcumltr3Nm { get; set; } = null!;

    public string AbsRsltSubUserAcumltr4Nm { get; set; } = null!;

    public string AbsRsltSubUserAcumltr5Nm { get; set; } = null!;

    public string AbsRsltSubUserAcumltr6Nm { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
