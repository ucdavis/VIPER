using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsCountryTblV
{
    public string Country { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public string Country2char { get; set; } = null!;

    public string EuMemberState { get; set; } = null!;

    public string PostSrchAvail { get; set; } = null!;

    public string AddrValidat { get; set; } = null!;

    public string EoSecPageName { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string EoAddrValClass { get; set; } = null!;

    public string EoAddrValPath { get; set; } = null!;

    public string EoAddrValMethod { get; set; } = null!;
}
