using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JpmFV
{
    public decimal JpmItemDKey { get; set; }

    public decimal JpmProfileDKey { get; set; }

    public decimal JpmCtlgDKey { get; set; }

    public decimal EmpDKey { get; set; }

    public decimal JpmFKey { get; set; }

    public decimal JpmFCnt { get; set; }

    public decimal EmpDCurKey { get; set; }

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? JpmFSvmRecIdx { get; set; }
}
