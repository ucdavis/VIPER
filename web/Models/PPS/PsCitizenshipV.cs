using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsCitizenshipV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string DependentId { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string CitizenshipStatus { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? CitizenSvmSeqNum { get; set; }

    public double? CitizenSvmSeqMrf { get; set; }

    public string? CitizenSvmPrimary { get; set; }
}
