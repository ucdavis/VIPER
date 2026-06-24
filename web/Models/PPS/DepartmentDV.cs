using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class DepartmentDV
{
    public decimal DeptDKey { get; set; }

    public string DeptSetId { get; set; } = null!;

    public string DeptId { get; set; } = null!;

    public DateTime DeptEffDt { get; set; }

    public string DeptEffStatCd { get; set; } = null!;

    public string DeptDesc { get; set; } = null!;

    public string DeptShortDesc { get; set; } = null!;

    public string DeptCmpyCd { get; set; } = null!;

    public string DeptSetIdLocCd { get; set; } = null!;

    public string DeptLocShortDesc { get; set; } = null!;

    public string DeptTxLocCd { get; set; } = null!;

    public string DeptBdgtLvlInd { get; set; } = null!;

    public string DeptEstabId { get; set; } = null!;

    public string DeptUseBdgtsInd { get; set; } = null!;

    public string DeptUseEncmbrncsInd { get; set; } = null!;

    public string DeptUseDstrbtnInd { get; set; } = null!;

    public string DeptBdgtId { get; set; } = null!;

    public string DeptUcTypeCd { get; set; } = null!;

    public string DeptUcRollupId { get; set; } = null!;

    public string DeptUcSauCd { get; set; } = null!;

    public string DeptUcLoc3Cd { get; set; } = null!;

    public string DeptUcSauDesc { get; set; } = null!;

    public string DeptUcLoc3Desc { get; set; } = null!;

    public string DeptBdgtLvlDesc { get; set; } = null!;

    public string DeptUcTypeDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? DeptSvmSeqNum { get; set; }

    public double? DeptSvmSeqMrf { get; set; }

    public string? DeptSvmIsMostrecent { get; set; }
}
