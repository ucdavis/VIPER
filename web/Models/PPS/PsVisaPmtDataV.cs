using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsVisaPmtDataV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string DependentId { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string VisaPermitType { get; set; } = null!;

    public DateTime Effdt { get; set; }

    public string VisaWrkpmtNbr { get; set; } = null!;

    public string VisaWrkpmtStatus { get; set; } = null!;

    public DateTime StatusDt { get; set; }

    public DateTime? DtIssued { get; set; }

    public string PlaceIssued { get; set; } = null!;

    public decimal DurationTime { get; set; }

    public string DurationType { get; set; } = null!;

    public DateTime? EntryDt { get; set; }

    public DateTime? ExpiratnDt { get; set; }

    public string IssuingAuthority { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
