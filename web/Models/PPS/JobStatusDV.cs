using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobStatusDV
{
    public decimal JobStatDKey { get; set; }

    public string JobStatCd { get; set; } = null!;

    public string JobStatDesc { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
