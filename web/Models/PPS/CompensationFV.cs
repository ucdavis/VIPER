using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class CompensationFV
{
    public decimal JobFKey { get; set; }

    public decimal CompFEffSeqNum { get; set; }

    public decimal CompRtDKey { get; set; }

    public decimal CompFKey { get; set; }

    public decimal CompDKey { get; set; }

    public decimal EmpDKey { get; set; }

    public decimal JobDKey { get; set; }

    public decimal EffDtKey { get; set; }

    public decimal CompRtPts { get; set; }

    public decimal CompRt { get; set; }

    public decimal CompPct { get; set; }

    public decimal CnvrtCompRt { get; set; }

    public decimal ChngAmt { get; set; }

    public decimal ChngPct { get; set; }

    public decimal ChngPts { get; set; }

    public decimal CompFCnt { get; set; }

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
