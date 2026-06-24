using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsJpmProfileV
{
    public string Deptid { get; set; } = null!;

    public string JpmProfileId { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string JpmJpPrflStatus { get; set; } = null!;

    public string JpmProfileUsage { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string JpmLgcyPrflId { get; set; } = null!;

    public string JpmJpType { get; set; } = null!;

    public string JpmOwnerEmplid { get; set; } = null!;

    public DateTime? StatusDt { get; set; }

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public string JpmSubscribe { get; set; } = null!;

    public string Descrshort { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
