using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsGpRsltAcumV
{
    public string? Deptid { get; set; }

    public string Emplid { get; set; } = null!;

    public string CalRunId { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public string GpPaygroup { get; set; } = null!;

    public string CalId { get; set; } = null!;

    public string OrigCalRunId { get; set; } = null!;

    public decimal RsltSegNum { get; set; }

    public decimal PinNum { get; set; }

    public decimal EmplRcdAcum { get; set; }

    public DateTime? AcmFromDt { get; set; }

    public DateTime? AcmThruDt { get; set; }

    public DateTime? SliceBgnDt { get; set; }

    public decimal SeqNum8 { get; set; }

    public DateTime? SliceEndDt { get; set; }

    public string UserKey1 { get; set; } = null!;

    public string UserKey2 { get; set; } = null!;

    public string UserKey3 { get; set; } = null!;

    public string UserKey4 { get; set; } = null!;

    public string UserKey5 { get; set; } = null!;

    public string UserKey6 { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string AcmType { get; set; } = null!;

    public string AcmPrdOptn { get; set; } = null!;

    public decimal CalcRsltVal { get; set; }

    public decimal CalcVal { get; set; }

    public decimal UserAdjVal { get; set; }

    public decimal PinParentNum { get; set; }

    public string CorrRtoInd { get; set; } = null!;

    public string ValidInSegInd { get; set; } = null!;

    public string CalledInSegInd { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
