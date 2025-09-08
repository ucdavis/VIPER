using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class UcdEmployeeFlagsDV
{
    public decimal EmpDKey { get; set; }

    public string EmpId { get; set; } = null!;

    public string? EmpWosempFlg { get; set; }

    public string? EmpAcdmcFlg { get; set; }

    public string? EmpAcdmcSenateFlg { get; set; }

    public string? EmpAcdmcFederationFlg { get; set; }

    public string? EmpTeachingFacultyFlg { get; set; }

    public string? EmpLadderRankFlg { get; set; }

    public string? EmpMspFlg { get; set; }

    public string? EmpSspFlg { get; set; }

    public string? EmpMspCareerPartialyrFlg { get; set; }

    public string? EmpMspCntrctFlg { get; set; }

    public string? EmpMspCasualFlg { get; set; }

    public string? EmpMspSeniorMgmtFlg { get; set; }

    public string? EmpSspCareerFlg { get; set; }

    public string? EmpSspCareerPartialyrFlg { get; set; }

    public string? EmpSspCntrctFlg { get; set; }

    public string? EmpSspCasualFlg { get; set; }

    public string? EmpSspCasualRestrictedFlg { get; set; }

    public string? EmpSspPerDiemFlg { get; set; }

    public string DdwMd5Type2 { get; set; } = null!;

    public string? SrcSysCd { get; set; }

    public DateTime DwRecInsrtDttm { get; set; }

    public string DwRecInsrtId { get; set; } = null!;

    public DateTime DwRecUpdtDttm { get; set; }

    public string DwRecUpdtId { get; set; } = null!;

    public DateTime SrcUpdtBtDttm { get; set; }

    public string? EmpSspFloaterFlg { get; set; }

    public string? EmpSupvrFlg { get; set; }

    public string? EmpMgrFlg { get; set; }

    public string? EmpMspCareerFlg { get; set; }

    public string? EmpAcdmcStdtFlg { get; set; }

    public string? EmpFacultyFlg { get; set; }

    public double? EmpFlgSvmSeqNum { get; set; }

    public double? EmpFlgSvmSeqMrf { get; set; }

    public string? EmpFlgSvmIsMostrecent { get; set; }
}
