using System;
using System.Collections.Generic;

namespace Viper.Models.PPS;

public partial class HistServiceCredit
{
    public int HistServiceCreditRecordId { get; set; }

    public string HistServiceCreditSchoolId { get; set; } = null!;

    public byte? HistServiceCreditMonthOffset { get; set; }

    public string HistServiceCreditHomeDept { get; set; } = null!;

    public string HistServiceCreditDeptName { get; set; } = null!;

    public string HistServiceCreditEmployeeId { get; set; } = null!;

    public string HistServiceCreditEmpName { get; set; } = null!;

    public string HistServiceCreditTitle { get; set; } = null!;

    public string HistServiceCreditProgram { get; set; } = null!;

    public short HistServiceCreditServiceMonths { get; set; }

    public short HistServiceCreditServiceYears { get; set; }

    public string HistServiceCreditAccrualDate { get; set; } = null!;

    public string HistServiceCreditOriginalHireDate { get; set; } = null!;

    public string HistServiceCreditCurrentAcrucode { get; set; } = null!;

    public string HistServiceCreditNextAcrucode { get; set; } = null!;

    public DateTime HistServiceCreditLastRunDate { get; set; }

    public string? HistServiceCreditCaoMailId { get; set; }

    public string? HistServiceCreditCaoMothraId { get; set; }

    public string? HistServiceCreditCaoDisplayName { get; set; }
}
