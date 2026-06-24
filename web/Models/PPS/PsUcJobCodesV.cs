using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcJobCodesV
{
    public string Setid { get; set; } = null!;

    public string Jobcode { get; set; } = null!;

    public DateTime? Effdt { get; set; }

    public string UcCtoOsCd { get; set; } = null!;

    public string UcAcaCompGrpCd { get; set; } = null!;

    public string UcRetmtSftyCd { get; set; } = null!;

    public string UcSyswdLcl { get; set; } = null!;

    public string UcByagrmnt { get; set; } = null!;

    public string UcEligibleOncall { get; set; } = null!;

    public string UcEligibleShiftd { get; set; } = null!;

    public string UcOffscale { get; set; } = null!;

    public string UcSummerSal { get; set; } = null!;

    public string UcBnExclElig { get; set; } = null!;

    public string ClassIndc { get; set; } = null!;

    public string UcFacultyIndc { get; set; } = null!;

    public string EmplClass { get; set; } = null!;

    public string UcOshpdCode { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string UcBnHwElig { get; set; } = null!;

    public double? UcjobcodeSvmSeqNum { get; set; }

    public double? UcjobcodeSvmSeqMrf { get; set; }

    public string? UcjobcodeSvmIsMostrecent { get; set; }
}
