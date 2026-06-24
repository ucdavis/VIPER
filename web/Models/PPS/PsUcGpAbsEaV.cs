using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsUcGpAbsEaV
{
    public string Emplid { get; set; } = null!;

    public decimal TransactionNbr { get; set; }

    public decimal PinTkNum { get; set; }

    public decimal PinTakeNum { get; set; }

    public DateTime BgnDt { get; set; }

    public DateTime EndDt { get; set; }

    public DateTime? ReturnDt { get; set; }

    public string UcAbsReason { get; set; } = null!;

    public decimal EmplRcd { get; set; }

    public DateTime LastDateWorked { get; set; }

    public decimal UcFmlaAdjhrs { get; set; }

    public string UcExcludeJob { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string ActionReason { get; set; } = null!;

    public string WfStatus { get; set; } = null!;

    public string UcAbsAddlvEn1 { get; set; } = null!;

    public decimal UcAbsAddlvPc1 { get; set; }

    public string UcAbsAddlvEn2 { get; set; } = null!;

    public decimal UcAbsAddlvPc2 { get; set; }

    public string UcAbsAddlvEn3 { get; set; } = null!;

    public decimal UcAbsAddlvPc3 { get; set; }

    public string UcAbsAddlvEn4 { get; set; } = null!;

    public decimal UcAbsAddlvPc4 { get; set; }

    public decimal UcAbsAddlvRgp { get; set; }

    public decimal UcAbsAddlvEsl { get; set; }

    public decimal UcAbsAddlvWcs { get; set; }

    public decimal UcAbsAddlvWcn { get; set; }

    public decimal UcAbsAddlvWcp { get; set; }

    public decimal UcAbsAddlvWcr { get; set; }

    public string UcAbsAddlvCff { get; set; } = null!;

    public decimal UcAbsAddlvSbp { get; set; }

    public decimal UcAbsAddlvSll { get; set; }

    public decimal UcAbsAddlvSls { get; set; }

    public decimal UcAbsSabCrdt { get; set; }

    public DateTime? PayBeginDt { get; set; }

    public DateTime? PayEndDt { get; set; }

    public DateTime? UcPayReturnDt { get; set; }

    public string Setid { get; set; } = null!;

    public string Deptid { get; set; } = null!;

    public string EmplClass { get; set; } = null!;

    public string GpAbsFmlaElig { get; set; } = null!;

    public string UcGpAbsCfraElg { get; set; } = null!;

    public string GpAbsFmlaOvrrd { get; set; } = null!;

    public string UcGpAbsCfraOvr { get; set; } = null!;

    public string Lastupdoprid { get; set; } = null!;

    public DateTime? Lastupddttm { get; set; }

    public DateTime CrBtDtm { get; set; }

    public double? CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double? UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;

    public string UcGpAbsPfcbOvr { get; set; } = null!;
}
