using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsPersonV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public DateTime? Birthdate { get; set; }

    public string Birthplace { get; set; } = null!;

    public string Birthcountry { get; set; } = null!;

    public string Birthstate { get; set; } = null!;

    public DateTime? DtOfDeath { get; set; }

    public DateTime? LastChildUpddtm { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? PersonSvmSeqNum { get; set; }

    public string? PersonSvmPrimary { get; set; }
}
