using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwEmployee
{
    public decimal EmpDKey { get; set; }

    public string EmpId { get; set; } = null!;

    public string EmpPpsUid { get; set; } = null!;

    public DateTime EmpEffDt { get; set; }

    public DateTime EmpExprDt { get; set; }

    public string EmpEffStatCd { get; set; } = null!;

    public string EmpPrmyFullNm { get; set; } = null!;

    public string EmpPrmyLastNm { get; set; } = null!;

    public string EmpPrmyFirstNm { get; set; } = null!;

    public string EmpWrkEmailAddrTxt { get; set; } = null!;

    public string EmpCtznshpCntryCd { get; set; } = null!;

    public string EmpCtznshpStatCd { get; set; } = null!;

    public string? EmpCtznshpDesc { get; set; }

    public string? EmpCtznshpCntryDesc { get; set; }

    public string EmpPrmyEthnctyGrpCd { get; set; } = null!;

    public string EmpPrmyEthnctyGrpDesc { get; set; } = null!;

    public string? EmpWosempFlg { get; set; }

    public string? EmpAcdmcFlg { get; set; }

    public string? EmpAcdmcSenateFlg { get; set; }

    public string? EmpAcdmcFederationFlg { get; set; }

    public string? EmpTeachingFacultyFlg { get; set; }

    public string? EmpLadderRankFlg { get; set; }

    public string? EmpMspFlg { get; set; }

    public string? EmpMspCareerFlg { get; set; }

    public string? EmpSspFlg { get; set; }

    public string? EmpSspCareerFlg { get; set; }

    public string? EmpMspSeniorMgmtFlg { get; set; }

    public string? EmpFacultyFlg { get; set; }
}
