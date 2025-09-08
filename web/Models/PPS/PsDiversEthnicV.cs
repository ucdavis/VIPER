using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsDiversEthnicV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string RegRegion { get; set; } = null!;

    public string EthnicGrpCd { get; set; } = null!;

    public string Setid { get; set; } = null!;

    public string ApsEcNdsAus { get; set; } = null!;

    public string PrimaryIndicator { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
