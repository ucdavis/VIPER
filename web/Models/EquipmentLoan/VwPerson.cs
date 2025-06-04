using System;
using System.Collections.Generic;

namespace Viper.Models.EquipmentLoan;

public partial class VwPerson
{
    public string Emplid { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string MiddleName { get; set; } = null!;

    public string? EmailAddr { get; set; }

    public string? CountryCode { get; set; }

    public string? CitizenshipStatusCode { get; set; }

    public string? CitizenshipStatus { get; set; }

    public string? Country { get; set; }

    public DateTime? Birthdate { get; set; }

    public DateTime? OrigHireDt { get; set; }

    public string? PpsId { get; set; }

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

    public string? EmpSspFloaterFlg { get; set; }

    public string? EmpSupvrFlg { get; set; }

    public string? EmpMgrFlg { get; set; }

    public string? EmpAcdmcStdtFlg { get; set; }
}
