using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsEmailAddressesV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string EAddrType { get; set; } = null!;

    public string EmailAddr { get; set; } = null!;

    public string PrefEmailFlag { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? EmailSvmSeqNum { get; set; }

    public double? EmailSvmSeqMrf { get; set; }

    public string? EmailSvmPrimaryEmail { get; set; }
}
