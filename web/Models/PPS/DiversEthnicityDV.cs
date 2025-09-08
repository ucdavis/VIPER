using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class DiversEthnicityDV
{
    public decimal EmpDKey { get; set; }

    public string EthnctyRgltnRegnCd { get; set; } = null!;

    public string EthnctyGrpCd { get; set; } = null!;

    public string EthnctySetId { get; set; } = null!;

    public string EthnctyPrmyEthnctyFlg { get; set; } = null!;

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }
}
