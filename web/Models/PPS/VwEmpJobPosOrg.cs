using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class VwEmpJobPosOrg
{
    public decimal EmpDKey { get; set; }

    public string? EmpId { get; set; }

    public string? EmpPpsUid { get; set; }

    public DateTime? EmpEffDt { get; set; }

    public DateTime? EmpExprDt { get; set; }

    public string? EmpEffStatCd { get; set; }

    public string? EmpPrmyFullNm { get; set; }

    public string? EmpPrmyLastNm { get; set; }

    public string? EmpPrmyFirstNm { get; set; }

    public string? EmpPrmyMidNm { get; set; }

    public string? EmpWrkEmailAddrTxt { get; set; }

    public string? EmpSexCd { get; set; }

    public string? EmpCtznshpCntryCd { get; set; }

    public string? EmpCtznshpStatCd { get; set; }

    public string? EmpCtznshpDesc { get; set; }

    public string? EmpCtznshpCntryDesc { get; set; }

    public string? EmpPrmyEthnctyGrpCd { get; set; }

    public string? EmpPrmyEthnctyGrpDesc { get; set; }

    public DateTime? EmpBirthDt { get; set; }

    public DateTime? EmpOrigHireDt { get; set; }

    public string? EmpWosempFlg { get; set; }

    public string? EmpAcdmcFlg { get; set; }

    public string? EmpAcdmcSenateFlg { get; set; }

    public string? EmpAcdmcFederationFlg { get; set; }

    public string? EmpTeachingFacultyFlg { get; set; }

    public string? EmpLadderRankFlg { get; set; }

    public string? EmpMspFlg { get; set; }

    public string? EmpMspCareerFlg { get; set; }

    public string? EmpMspCareerPartialyrFlg { get; set; }

    public string? EmpMspCntrctFlg { get; set; }

    public string? EmpMspCasualFlg { get; set; }

    public string? EmpMspSeniorMgmtFlg { get; set; }

    public string? EmpSspFlg { get; set; }

    public string? EmpSspCareerFlg { get; set; }

    public string? EmpSspCareerPartialyrFlg { get; set; }

    public string? EmpSspCntrctFlg { get; set; }

    public string? EmpSspCasualFlg { get; set; }

    public string? EmpSspCasualRestrictedFlg { get; set; }

    public string? EmpSspPerDiemFlg { get; set; }

    public string? EmpFacultyFlg { get; set; }

    public decimal JobFKey { get; set; }

    public decimal? JobDKey { get; set; }

    public DateTime EffDt { get; set; }

    public DateTime ExpctdEndDate { get; set; }

    public decimal? JobFEmpRecNum { get; set; }

    public decimal? JobFEffSeqNum { get; set; }

    public decimal? UnionDKey { get; set; }

    public decimal? DeptDKey { get; set; }

    public decimal? OrgDJobKey { get; set; }

    public decimal? OrgDJobCurKey { get; set; }

    public string? JobCd { get; set; }

    public string? JobCdDesc { get; set; }

    public string? JobCdShortDesc { get; set; }

    public string? JobCdOcuptnlSubgrpCd { get; set; }

    public string? JobCdOcuptnlSubgrpDesc { get; set; }

    public string? JobCdFlsaStatCd { get; set; }

    public string? JobCdUnionCd { get; set; }

    public string? JobCdUnionDesc { get; set; }

    public string DeptCd { get; set; } = null!;

    public string? DeptTtl { get; set; }

    public string? DeptShrtTtl { get; set; }

    public string? SubDivTtl { get; set; }

    public string SubDivCd { get; set; } = null!;

    public string? JobStatCd { get; set; }

    public string? JobStatDesc { get; set; }

    public decimal? JobFAnnlRt { get; set; }

    public decimal? JobFMthlyRt { get; set; }

    public decimal? JobFHrlyRt { get; set; }

    public decimal? JobFFtePct { get; set; }

    public decimal? JobStatDEmpKey { get; set; }

    public decimal? JobStatDHrKey { get; set; }

    public string? JobFSvmPrimaryIdx { get; set; }

    public int Isprimary { get; set; }

    public int EffDateActive { get; set; }

    public string JobStatus { get; set; } = null!;

    public DateTime? JobUpdateTimestamp { get; set; }

    public decimal? PosnDKey { get; set; }

    public string? PosnNum { get; set; }

    public DateTime? PosnEffDt { get; set; }

    public string? PosnEffStatCd { get; set; }

    public string? PosnDesc { get; set; }

    public string? PosnDeptCd { get; set; }

    public string? PosnDeptDesc { get; set; }

    public string? PosnJobCd { get; set; }

    public string? PosnJobCdDesc { get; set; }

    public string? PosnUnionCd { get; set; }

    public string? PosnGrdCd { get; set; }

    public string? PosnClassInd { get; set; }
}
