using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class PsGpPinV
{
    public decimal PinNum { get; set; }

    public string PinCode { get; set; } = null!;

    public string PinNm { get; set; } = null!;

    public string PinType { get; set; } = null!;

    public string Descr { get; set; } = null!;

    public string FldFmt { get; set; } = null!;

    public string DefnAsofdtOptn { get; set; } = null!;

    public string CheckGenerInd { get; set; } = null!;

    public string RecalcInd { get; set; } = null!;

    public string UsedBy { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string PinCategory { get; set; } = null!;

    public string PinIndustry { get; set; } = null!;

    public string PinOwner { get; set; } = null!;

    public string PinClass { get; set; } = null!;

    public string OvrdIndCal { get; set; } = null!;

    public string OvrdIndPyent { get; set; } = null!;

    public string OvrdIndPg { get; set; } = null!;

    public string OvrdIndPye { get; set; } = null!;

    public string OvrdIndPi { get; set; } = null!;

    public string OvrdIndElem { get; set; } = null!;

    public string OvrdIndSovr { get; set; } = null!;

    public string StoreRslt { get; set; } = null!;

    public string StoreRsltIfZero { get; set; } = null!;

    public decimal PinParentNum { get; set; }

    public string PinCustom1 { get; set; } = null!;

    public string PinCustom2 { get; set; } = null!;

    public string PinCustom3 { get; set; } = null!;

    public string PinCustom4 { get; set; } = null!;

    public string PinCustom5 { get; set; } = null!;

    public string FcstInd { get; set; } = null!;

    public string FcstReqInd { get; set; } = null!;

    public decimal PinDriverNum { get; set; }

    public string EntryTypeUserF1 { get; set; } = null!;

    public decimal PinUserFld1Num { get; set; }

    public string EntryTypeUserF2 { get; set; } = null!;

    public decimal PinUserFld2Num { get; set; }

    public string EntryTypeUserF3 { get; set; } = null!;

    public decimal PinUserFld3Num { get; set; }

    public string EntryTypeUserF4 { get; set; } = null!;

    public decimal PinUserFld4Num { get; set; }

    public string EntryTypeUserF5 { get; set; } = null!;

    public decimal PinUserFld5Num { get; set; }

    public string EntryTypeUserF6 { get; set; } = null!;

    public decimal PinUserFld6Num { get; set; }

    public string RtoDeltaUfLvl { get; set; } = null!;

    public DateTime? LastUpdtDttm { get; set; }

    public string LastUpdtOprid { get; set; } = null!;

    public string AutoAssignedType { get; set; } = null!;

    public string GpVersion { get; set; } = null!;

    public DateTime CrBtDtm { get; set; }

    public double CrBtNbr { get; set; }

    public DateTime UpdBtDtm { get; set; }

    public double UpdBtNbr { get; set; }

    public double OdsVrsnNbr { get; set; }

    public string DmlInd { get; set; } = null!;
}
