using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsAddressTypTblV
{
    public string AddressType { get; set; } = null!;

    public decimal OrderBySeq { get; set; }

    public string AddrTypeDescr { get; set; } = null!;

    public string AddrTypeShort { get; set; } = null!;

    public string DataTypeCd { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
