using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class CompensationDV
{
    public decimal CompDKey { get; set; }

    public string CompFreqCd { get; set; } = null!;

    public string CompFreqDesc { get; set; } = null!;

    public string ManlSwtchFlg { get; set; } = null!;

    public string RtCdGrpCd { get; set; } = null!;

    public string FteIndFlg { get; set; } = null!;

    public string CompntSrcCd { get; set; } = null!;

    public string CompntSrcDesc { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
