using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsGpAbsEaV
{
    public string Deptid { get; set; } = null!;

    public string Emplid { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public decimal TransactionNbr { get; set; }

    public decimal PinTakeNum { get; set; }

    public DateTime BgnDt { get; set; }

    public DateTime EndDt { get; set; }

    public DateTime? ReturnDt { get; set; }

    public DateTime RequestDt { get; set; }

    public string AbsEntrySrc { get; set; } = null!;

    public string AbsenceReason { get; set; } = null!;

    public string ManagerApprInd { get; set; } = null!;

    public DateTime? LastUpdtDt { get; set; }

    public string WfStatus { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
