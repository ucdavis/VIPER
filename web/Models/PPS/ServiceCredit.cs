using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class ServiceCredit
{
    public int ServiceCreditRecordId { get; set; }

    public string ServiceCreditSchoolId { get; set; } = null!;

    public byte? ServiceCreditMonthOffset { get; set; }

    public string ServiceCreditHomeDept { get; set; } = null!;

    public string ServiceCreditDeptName { get; set; } = null!;

    public string ServiceCreditEmployeeId { get; set; } = null!;

    public string ServiceCreditEmpName { get; set; } = null!;

    public string ServiceCreditTitle { get; set; } = null!;

    public string ServiceCreditProgram { get; set; } = null!;

    public short ServiceCreditServiceMonths { get; set; }

    public short ServiceCreditServiceYears { get; set; }

    public string ServiceCreditAccrualDate { get; set; } = null!;

    public string ServiceCreditOriginalHireDate { get; set; } = null!;

    public string ServiceCreditCurrentAcrucode { get; set; } = null!;

    public string ServiceCreditNextAcrucode { get; set; } = null!;

    public DateTime ServiceCreditLastRunDate { get; set; }

    public string? ServiceCreditCaoMailId { get; set; }

    public string? ServiceCreditCaoMothraId { get; set; }

    public string? ServiceCreditCaoDisplayName { get; set; }
}
