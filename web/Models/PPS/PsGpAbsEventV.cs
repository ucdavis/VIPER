using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsGpAbsEventV
{
    public string? Deptid { get; set; }

    public string Emplid { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public DateTime BgnDt { get; set; }

    public decimal PinTakeNum { get; set; }

    public DateTime EndDt { get; set; }

    public DateTime? OrigBeginDt { get; set; }

    public string AbsEntrySrc { get; set; } = null!;

    public string PrcEvtActnOptn { get; set; } = null!;

    public string VoidedInd { get; set; } = null!;

    public string AbsenceReason { get; set; } = null!;

    public decimal BeginDayHrs { get; set; }

    public string BeginDayHalfInd { get; set; } = null!;

    public decimal EndDayHrs { get; set; }

    public string EndDayHalfInd { get; set; } = null!;

    public string AllDaysInd { get; set; } = null!;

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public DateTime? StartTime2 { get; set; }

    public DateTime? EndTime2 { get; set; }

    public string EvtConfig1 { get; set; } = null!;

    public string EvtConfig2 { get; set; } = null!;

    public string EvtConfig3 { get; set; } = null!;

    public string EvtConfig4 { get; set; } = null!;

    public DateTime? EvtConfig1Dt { get; set; }

    public DateTime? EvtConfig2Dt { get; set; }

    public DateTime? EvtConfig3Dt { get; set; }

    public DateTime? EvtConfig4Dt { get; set; }

    public decimal EvtConfig1Dec { get; set; }

    public decimal EvtConfig2Dec { get; set; }

    public decimal EvtConfig3Dec { get; set; }

    public decimal EvtConfig4Dec { get; set; }

    public decimal EvtConfig1Mon { get; set; }

    public decimal EvtConfig2Mon { get; set; }

    public decimal EvtConfig3Mon { get; set; }

    public decimal EvtConfig4Mon { get; set; }

    public string CurrencyCd1 { get; set; } = null!;

    public string CurrencyCd2 { get; set; } = null!;

    public string CurrencyCd3 { get; set; } = null!;

    public string CurrencyCd4 { get; set; } = null!;

    public string ManagerApprInd { get; set; } = null!;

    public decimal OvrdEntVal { get; set; }

    public decimal OvrdAdjVal { get; set; }

    public string CalRunId { get; set; } = null!;

    public decimal PyeRunNum { get; set; }

    public DateTime? LastUpdtDt { get; set; }

    public DateTime? ProcessDt { get; set; }

    public string AbsEvtFcstVal { get; set; } = null!;

    public DateTime? FcstDttm { get; set; }

    public decimal DurationAbs { get; set; }

    public decimal DurationDys { get; set; }

    public decimal DurationHours { get; set; }

    public DateTime? ActionDtSs { get; set; }

    public string WfStatus { get; set; } = null!;

    public decimal TransactionNbr { get; set; }

    public decimal TransactionNbrEa { get; set; }

    public DateTime? FirstProcDt { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string? AbsCanReason { get; set; }
}
