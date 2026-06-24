using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class CompensationDRateV
{
    public decimal CompRtDKey { get; set; }

    public string CompRtCd { get; set; } = null!;

    public DateTime CompRtEffDt { get; set; }

    public string CompRtEffStatCd { get; set; } = null!;

    public string CompRtDesc { get; set; } = null!;

    public string CompRtShortDesc { get; set; } = null!;

    public string CompBasePaySwtchCd { get; set; } = null!;

    public string UseHighstRtSwtchCd { get; set; } = null!;

    public string CompRtTypeCd { get; set; } = null!;

    public string CompRtTypeDesc { get; set; } = null!;

    public decimal CompRt { get; set; }

    public decimal CompPct { get; set; }

    public string CompFreqCd { get; set; } = null!;

    public string CompFreqDesc { get; set; } = null!;

    public string CompEarnCd { get; set; } = null!;

    public string CompEarnDesc { get; set; } = null!;

    public string FteIndFlg { get; set; } = null!;

    public string RtClassCd { get; set; } = null!;

    public string SnrtyCalcFlg { get; set; } = null!;

    public string SlryPkgWarnFlg { get; set; } = null!;

    public string CmpNonUpdFlg { get; set; } = null!;

    public string CmpCalcByCd { get; set; } = null!;

    public string CmpCalcByDesc { get; set; } = null!;

    public string CmpPaySwtchFlg { get; set; } = null!;

    public string LookupId { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
