using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class EmployeeDV
{
    public decimal EmpDKey { get; set; }

    public string EmpId { get; set; } = null!;

    public DateTime EmpEffDt { get; set; }

    public DateTime EmpExprDt { get; set; }

    public string EmpEffStatCd { get; set; } = null!;

    public string EmpPpsUid { get; set; } = null!;

    public DateTime EmpBirthDt { get; set; }

    public string EmpSexCd { get; set; } = null!;

    public string EmpSexDesc { get; set; } = null!;

    public string EmpHighEduLvlCd { get; set; } = null!;

    public string EmpHighEduLvlDesc { get; set; } = null!;

    public string EmpFullTimeStdntCd { get; set; } = null!;

    public string EmpPrmyNmSufxCd { get; set; } = null!;

    public string EmpPrmyFullNm { get; set; } = null!;

    public string EmpPrmyLastNm { get; set; } = null!;

    public string EmpPrmyFirstNm { get; set; } = null!;

    public string EmpPrmyMidNm { get; set; } = null!;

    public string EmpHmCntryCd { get; set; } = null!;

    public string EmpWrkAddr1Txt { get; set; } = null!;

    public string EmpWrkAddr2Txt { get; set; } = null!;

    public string EmpWrkCityNm { get; set; } = null!;

    public string EmpWrkStCd { get; set; } = null!;

    public string EmpWrkPstlCd { get; set; } = null!;

    public string EmpWrkPhNum { get; set; } = null!;

    public string EmpHmAddr1Txt { get; set; } = null!;

    public string EmpHmAddr2Txt { get; set; } = null!;

    public string EmpHmCityNm { get; set; } = null!;

    public string EmpHmStCd { get; set; } = null!;

    public string EmpHmPstlCd { get; set; } = null!;

    public string EmpWrkCntryCd { get; set; } = null!;

    public string EmpWrkCntryDesc { get; set; } = null!;

    public string EmpWrkEmailAddrTxt { get; set; } = null!;

    public string EmpUcAddrReleaseFlg { get; set; } = null!;

    public string EmpUcPhReleaseFlg { get; set; } = null!;

    public string EmpSpouseFullNm { get; set; } = null!;

    public string EmpCtznshpCntryCd { get; set; } = null!;

    public string EmpCtznshpStatCd { get; set; } = null!;

    public string? EmpCtznshpDesc { get; set; }

    public string? EmpCtznshpCntryDesc { get; set; }

    public DateTime EmpOrigHireDt { get; set; }

    public string EmpUsWrkElgbltyCd { get; set; } = null!;

    public string EmpUsWrkElgbltyDesc { get; set; } = null!;

    public string EmpPrmyEthnctyGrpCd { get; set; } = null!;

    public string EmpPrmyEthnctyGrpDesc { get; set; } = null!;

    public string DdwMd5Type2 { get; set; } = null!;

    public string SrcSysCd { get; set; } = null!;

    public DateTime DwRecInsrtDttm { get; set; }

    public DateTime DwRecUpdtDttm { get; set; }

    public DateTime SrcUpdtBtDttm { get; set; }

    public double? EmpSvmSeqNum { get; set; }

    public double? EmpSvmSeqMrf { get; set; }

    public string? EmpSvmIsMostrecent { get; set; }
}
