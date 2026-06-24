using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class OdsEmployeeFlagsDV
{
    public string Deptid { get; set; } = null!;

    public string EmpId { get; set; } = null!;

    public string EmpWosempFlg { get; set; } = null!;

    public string EmpAcdmcFlg { get; set; } = null!;

    public string EmpAcdmcSenateFlg { get; set; } = null!;

    public string EmpAcdmcFederationFlg { get; set; } = null!;

    public string EmpFacultyFlg { get; set; } = null!;

    public string EmpTeachingFacultyFlg { get; set; } = null!;

    public string EmpLadderRankFlg { get; set; } = null!;

    public string EmpMgrFlg { get; set; } = null!;

    public string EmpMspFlg { get; set; } = null!;

    public string EmpSspFlg { get; set; } = null!;

    public string EmpSupvrFlg { get; set; } = null!;

    public string EmpMspCareerFlg { get; set; } = null!;

    public string EmpMspCareerPartialyrFlg { get; set; } = null!;

    public string EmpMspCntrctFlg { get; set; } = null!;

    public string EmpMspCasualFlg { get; set; } = null!;

    public string EmpMspSeniorMgmtFlg { get; set; } = null!;

    public string EmpSspCareerFlg { get; set; } = null!;

    public string EmpSspCareerPartialyrFlg { get; set; } = null!;

    public string EmpSspCntrctFlg { get; set; } = null!;

    public string EmpSspCasualFlg { get; set; } = null!;

    public string EmpSspCasualRestrictedFlg { get; set; } = null!;

    public string EmpSspPerDiemFlg { get; set; } = null!;

    public string EmpSspFloaterFlg { get; set; } = null!;

    public string EmpAcdmcStdtFlg { get; set; } = null!;

    public DateTime LoadDt { get; set; }

    public string LoadId { get; set; } = null!;

    public double? FlagsSvmSeqNum { get; set; }

    public string? FlagsSvmPrimary { get; set; }
}
