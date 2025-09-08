using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsAddressesV
{
    public string? Deptid { get; set; }

    public string Emplid { get; set; } = null!;

    public string AddressType { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string EffStatus { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string Address1 { get; set; } = null!;

    public string Address2 { get; set; } = null!;

    public string Address3 { get; set; } = null!;

    public string Address4 { get; set; } = null!;

    public string City { get; set; } = null!;

    public string HouseType { get; set; } = null!;

    public string County { get; set; } = null!;

    public string State { get; set; } = null!;

    public string Postal { get; set; } = null!;

    public string RegRegion { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? AddressSvmSeqNum { get; set; }

    public double? AddressSvmSeqMrf { get; set; }

    public string? AddressSvmPrimary { get; set; }
}
