using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsPerOrgInstV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public decimal OrgInstanceErn { get; set; }

    public string PerOrg { get; set; } = null!;

    public string PoiType { get; set; } = null!;

    public string OrigHireOvr { get; set; } = null!;

    public DateTime? OrigHireDt { get; set; }

    public string OrgInstSrvOvr { get; set; } = null!;

    public DateTime? OrgInstSrvDt { get; set; }

    public string NeeProviderId { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public string Lastupdoprid { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public double? PerorgSvmSeqNum { get; set; }

    public double? PerorgSvmSeqMrf { get; set; }

    public string? PerorgSvmIsMostrecent { get; set; }
}
