using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class AbsenceResultFV
{
    public decimal EmpDKey { get; set; }

    public decimal AbsCalDKey { get; set; }

    public decimal AbsRsltFEmpRecNum { get; set; }

    public string AbsRsltFOrigCalRunId { get; set; } = null!;

    public string AbsRsltFCalRunId { get; set; } = null!;

    public decimal AbsRsltFRsltSgmntNum { get; set; }

    public decimal PinDKey { get; set; }

    public decimal AbsRsltFAcumEmpRecNum { get; set; }

    public decimal AcumFromDtKey { get; set; }

    public decimal AcumThrghDtKey { get; set; }

    public decimal SliceBegDtKey { get; set; }

    public decimal AbsRsltFSeq8Num { get; set; }

    public decimal AbsRsltFKey { get; set; }

    public decimal ParentPinDKey { get; set; }

    public decimal AbsRsltDKey { get; set; }

    public decimal JobDKey { get; set; }

    public decimal SliceEndDtKey { get; set; }

    public decimal JobFKey { get; set; }

    public decimal AbsRsltFCalcRsltValRt { get; set; }

    public decimal AbsRsltFCalcValRt { get; set; }

    public decimal AbsRsltFUserAdjstValRt { get; set; }

    public decimal AbsRsltFCnt { get; set; }

    public string DdwMd5Type1 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? AbsRsltFSvmSeqNum { get; set; }

    public double? AbsRsltFSvmSeqMrf { get; set; }

    public string? AbsRsltFSvmIsMostrecent { get; set; }
}
