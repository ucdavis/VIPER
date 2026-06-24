using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class JobActionDV
{
    public decimal JobActnDKey { get; set; }

    public string JobActnCd { get; set; } = null!;

    public string JobActnRsnCd { get; set; } = null!;

    public DateTime JobActnEffDt { get; set; }

    public string JobActnDesc { get; set; } = null!;

    public string JobActnRsnDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
