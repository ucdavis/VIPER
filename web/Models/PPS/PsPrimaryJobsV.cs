using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsPrimaryJobsV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public string PrimaryJobApp { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public DateTime Effdt { get; set; }

    public string PrimaryJobInd { get; set; } = null!;

    public string PrimaryFlag1 { get; set; } = null!;

    public string PrimaryFlag2 { get; set; } = null!;

    public string PrimaryJobsSrc { get; set; } = null!;

    public decimal JobEffseq { get; set; }

    public decimal JobEmplRcd { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? PriJobsSvmSeqNum { get; set; }

    public double? PriJobsSvmSeqMrf { get; set; }

    public string? PriJobsSvmIsMostrecent { get; set; }
}
