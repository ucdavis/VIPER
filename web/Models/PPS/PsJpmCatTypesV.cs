using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsJpmCatTypesV
{
    public string JpmCatType { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string SystemDataFlg { get; set; } = null!;

    public string JpmAdhocFlg { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
