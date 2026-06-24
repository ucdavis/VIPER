using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AbsenceEventFV
{
    public DateTime BegDt { get; set; }

    public DateTime EndDt { get; set; }

    public decimal? EmpDKey { get; set; }

    public decimal? AbsEvntFEmpRecNum { get; set; }

    public decimal? BegDtKey { get; set; }

    public decimal? PinDKey { get; set; }

    public decimal? EndDtKey { get; set; }

    public decimal? AbsEvntFKey { get; set; }

    public decimal? AbsEvntDKey { get; set; }

    public decimal? JobDKey { get; set; }

    public decimal? EventCnfgrtn1DtKey { get; set; }

    public decimal? EventCnfgrtn2DtKey { get; set; }

    public decimal? EventCnfgrtn3DtKey { get; set; }

    public decimal? EventCnfgrtn4DtKey { get; set; }

    public decimal? OrigBegDtKey { get; set; }

    public decimal? PrcsDtKey { get; set; }

    public decimal? FirstPrcsDtKey { get; set; }

    public decimal? LastUpdtDtKey { get; set; }

    public decimal? JobFKey { get; set; }

    public decimal? AbsEvntFPayeeRunNum { get; set; }

    public decimal? AbsEvntFCnt { get; set; }

    public decimal? AbsEvntFOvrdEntlmntAmt { get; set; }

    public decimal? AbsEvntFOvrdAdjstmtAmt { get; set; }

    public string? AbsEvntFCalRunId { get; set; }

    public decimal? AbsEvntFCnfgrtn1Num { get; set; }

    public decimal? AbsEvntFCnfgrtn2Num { get; set; }

    public decimal? AbsEvntFCnfgrtn3Num { get; set; }

    public decimal? AbsEvntFCnfgrtn4Num { get; set; }

    public decimal? AbsEvntFDurtnAmt { get; set; }

    public decimal? AbsEvntFDurtnDys { get; set; }

    public decimal? AbsEvntFDurtnHrs { get; set; }

    public string? DdwMd5Type1 { get; set; }

    public string? SrcSysCd { get; set; }

    public DateTime? DwRecInsrtDttm { get; set; }

    public string? DwRecInsrtId { get; set; }

    public DateTime? DwRecUpdtDttm { get; set; }

    public string? DwRecUpdtId { get; set; }

    public DateTime? SrcUpdtBtDttm { get; set; }

    public double? AbsFSvmSeqNum { get; set; }

    public double? AbsFSvmSeqMrf { get; set; }

    public string? AbsFSvmIsMostrecent { get; set; }
}
