using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsEthnicGrpTblV
{
    public string Setid { get; set; } = null!;

    public string EthnicGrpCd { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Descr50 { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string EthnicCategory { get; set; } = null!;

    public string EthnicGroup { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
