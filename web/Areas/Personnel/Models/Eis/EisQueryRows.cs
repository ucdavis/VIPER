using System.ComponentModel.DataAnnotations.Schema;

namespace Viper.Areas.Personnel.Models.Eis;

// Result rows of the legacy usp_eis_* procedures, read with Database.SqlQuery. Each class lists
// every column its procedure returns, with the procedure's own column names, so EF can map the
// whole result. Types follow sys.dm_exec_describe_first_result_set on the dev database.

/// <summary>usp_eis_getPersonnelList and usp_eis_getPersonnelListUnit.</summary>
public sealed class EisPersonRow
{
    [Column("EMPLOYEE_ID")] public string EmployeeId { get; set; } = string.Empty;
    [Column("EMP_NAME")] public string Name { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonName.</summary>
public sealed class EisNameRow
{
    [Column("EMP_NAME")] public string Name { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonDOB: the date of birth as the procedure formats it.</summary>
public sealed class EisDateOfBirthRow
{
    [Column("DOB")] public string? DateOfBirth { get; set; }
}

/// <summary>usp_eis_getPersonAge.</summary>
public sealed class EisAgeRow
{
    [Column("AGE")] public int? Age { get; set; }
}

/// <summary>usp_eis_getPersonGender.</summary>
public sealed class EisGenderRow
{
    [Column("GENDER")] public string Gender { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonEthnicity.</summary>
public sealed class EisEthnicityRow
{
    [Column("ETHNICITY")] public string Ethnicity { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonHireDate.</summary>
public sealed class EisHireDateRow
{
    [Column("HIRE_DATE")] public DateTime? HireDate { get; set; }
}

/// <summary>usp_eis_getPersonEmpStatus.</summary>
public sealed class EisEmploymentStatusRow
{
    [Column("EMP_STATUS")] public string Status { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonCitizenship.</summary>
public sealed class EisCitizenshipRow
{
    [Column("CITIZEN_CODE")] public string Code { get; set; } = string.Empty;
    [Column("CITIZENSHIP")] public string Citizenship { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonVisa.</summary>
public sealed class EisVisaRow
{
    [Column("VISA_TYPE")] public string VisaType { get; set; } = string.Empty;
    [Column("VISADESCRIPTION")] public string Description { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonPrimaryAffiliation.</summary>
public sealed class EisAffiliationRow
{
    [Column("AFFILIATION")] public string? Affiliation { get; set; }
}

/// <summary>usp_eis_getPersonPrimaryTitle.</summary>
public sealed class EisTitleRow
{
    [Column("TITLE")] public string Title { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonJobGroup.</summary>
public sealed class EisJobGroupRow
{
    [Column("JOBGROUP")] public string JobGroup { get; set; } = string.Empty;
}

/// <summary>
/// The procedures that return a single NAME column: usp_eis_getPersonDepartment,
/// usp_eis_getPersonRelUnit, usp_eis_getPersonStaffProgram and usp_eis_getPersonFacultyProgram.
/// </summary>
public sealed class EisNamedValueRow
{
    [Column("NAME")] public string? Name { get; set; }
}

/// <summary>usp_eis_getPersonCBUC: the collective bargaining unit code.</summary>
public sealed class EisBargainingUnitRow
{
    [Column("CBUC")] public string BargainingUnit { get; set; } = string.Empty;
}

/// <summary>usp_eis_isStaff and usp_eis_isFaculty.</summary>
public sealed class EisCountRow
{
    [Column("THISCOUNT")] public int? Count { get; set; }
}

/// <summary>usp_eis_getPersonStaffStatus.</summary>
public sealed class EisStaffStatusRow
{
    [Column("STATUS")] public string? Status { get; set; }
}

/// <summary>usp_eis_isLadderRank.</summary>
public sealed class EisLadderRankRow
{
    [Column("LADDER_RANK")] public string LadderRank { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonServiceCredit: service credit in months.</summary>
public sealed class EisServiceCreditRow
{
    [Column("CREDIT")] public decimal Credit { get; set; }
}

/// <summary>usp_eis_getPersonLeaveBalances: balances in hours.</summary>
public sealed class EisLeaveBalanceRow
{
    [Column("vacBalance")] public decimal? Vacation { get; set; }
    [Column("sickBalance")] public decimal? Sick { get; set; }
    [Column("ptoBalance")] public decimal? Pto { get; set; }
}

/// <summary>usp_eis_getPersonAppt. Appointment dates come back as text.</summary>
public sealed class EisAppointmentRow
{
    [Column("EMPLOYEE_ID")] public string EmployeeId { get; set; } = string.Empty;
    [Column("APPT_NUM")] public string? Number { get; set; }
    [Column("TITLE_CODE")] public string TitleCode { get; set; } = string.Empty;
    [Column("JGT_JOB_GROUP_ID")] public string JobGroupId { get; set; } = string.Empty;
    [Column("APPT_BEGIN_DATE")] public string? BeginDate { get; set; }
    [Column("GRADE")] public string? Grade { get; set; }
    [Column("APPT_END_DATE")] public string? EndDate { get; set; }
    [Column("APPT_TITLE")] public string Title { get; set; } = string.Empty;
    [Column("APPT_DEPT")] public string Department { get; set; } = string.Empty;
    [Column("EXEMPT_STATUS")] public string ExemptStatus { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonDistRecords: one appointment's distributions.</summary>
public sealed class EisDistributionRow
{
    [Column("EMPLOYEE_ID")] public string EmployeeId { get; set; } = string.Empty;
    [Column("APPT_NUM")] public string AppointmentNumber { get; set; } = string.Empty;
    [Column("DIST_NUM")] public double? Number { get; set; }
    [Column("DIST_STEP")] public decimal Step { get; set; }
    [Column("DIST_BEGIN_DATE")] public DateTime BeginDate { get; set; }
    [Column("DIST_END_DATE")] public DateTime? EndDate { get; set; }
    [Column("DIST_PERCENT")] public decimal? Percent { get; set; }
    [Column("DIST_PAYRATE")] public decimal PayRate { get; set; }
    [Column("DIST_DOS")] public string? DosCode { get; set; }
    [Column("ACCOUNT")] public string? Account { get; set; }
    [Column("ANNUAL_AMOUNT")] public decimal AnnualAmount { get; set; }
    [Column("EXCLUDED")] public int? Excluded { get; set; }
}

/// <summary>PPS.dbo.vw_stipends, limited to the columns the Appointments page shows.</summary>
public sealed class EisStipendRow
{
    [Column("EFFDT")] public DateTime EffectiveDate { get; set; }
    [Column("EARNINGS_END_DT")] public DateTime? EndDate { get; set; }
    [Column("ERNCD")] public string EarningsCode { get; set; } = string.Empty;
    [Column("OTH_PAY")] public decimal Pay { get; set; }
    [Column("ADDL_PAY_FREQUENCY")] public string PayFrequency { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonHistory: appointment history from the UCPath warehouse.</summary>
public sealed class EisHistoryRow
{
    [Column("actionDate")] public DateTime ActionDate { get; set; }
    [Column("title")] public string Title { get; set; } = string.Empty;
    [Column("titlecode")] public string TitleCode { get; set; } = string.Empty;
    [Column("apptDepartment")] public string Department { get; set; } = string.Empty;
    [Column("apptBeginDate")] public DateTime BeginDate { get; set; }
    [Column("apptEndDate")] public DateTime? EndDate { get; set; }
    [Column("distStep")] public decimal Step { get; set; }
    [Column("payRate")] public decimal PayRate { get; set; }
    [Column("apptPercent")] public decimal Percent { get; set; }
    [Column("actionComment")] public string Comment { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonPPSHistory: appointment history from the retired PPS system.</summary>
public sealed class EisPpsHistoryRow
{
    [Column("actionDate")] public DateTime? ActionDate { get; set; }
    [Column("title")] public string? Title { get; set; }
    [Column("titleCode")] public string? TitleCode { get; set; }
    [Column("apptDepartment")] public string? Department { get; set; }
    [Column("apptBeginDate")] public DateTime? BeginDate { get; set; }
    [Column("apptEndDate")] public DateTime? EndDate { get; set; }
    [Column("distStep")] public string? Step { get; set; }
    [Column("payRate")] public decimal? PayRate { get; set; }
    [Column("apptPercent")] public decimal? Percent { get; set; }
    [Column("actionComment")] public string? Comment { get; set; }
}

/// <summary>usp_eis_getPersonLOA and usp_eis_getPersonPPSLOA: leaves of absence.</summary>
public sealed class EisLeaveRow
{
    [Column("LOA_BEGIN_DATE")] public DateTime? BeginDate { get; set; }
    [Column("LOA_RETURN_DATE")] public DateTime? ReturnDate { get; set; }
    [Column("LOA_DESCRIPTION")] public string? Description { get; set; }
}

/// <summary>usp_eis_getPersonPermanentAddress.</summary>
public sealed class EisPermanentAddressRow
{
    [Column("HOME_ADDR_RLSE")] public string ReleaseCampus { get; set; } = string.Empty;
    [Column("EMP_ORG_ADDR_RLSE")] public string ReleaseOrganization { get; set; } = string.Empty;
    [Column("ADDRESS_LINE1")] public string Line1 { get; set; } = string.Empty;
    [Column("ADDRESS_LINE2")] public string Line2 { get; set; } = string.Empty;
    [Column("ADDRESS_CITY")] public string City { get; set; } = string.Empty;
    [Column("ADDRESS_STATE")] public string State { get; set; } = string.Empty;
    [Column("ADDRESS_ZIP")] public string Zip { get; set; } = string.Empty;
}

/// <summary>usp_eis_getPersonHomePhone. Its release flags are numbers, unlike the address's.</summary>
public sealed class EisHomePhoneRow
{
    [Column("HOME_PHONE")] public string Phone { get; set; } = string.Empty;
    [Column("HOME_PHONE_RLSE")] public int? ReleaseCampus { get; set; }
    [Column("EMP_ORG_PHONE_RLSE")] public int? ReleaseOrganization { get; set; }
}

/// <summary>The AAUD identifiers EIS needs for one employee.</summary>
public sealed record EisPersonIds(string? MothraId, string? MailId, string? PpsId, int? MivId = null);

/// <summary>
/// One of the employee's jobs from PPS.dbo.vw_PersonJobPosition, with the conditions the legacy
/// category procedures tested computed in SQL, so every type is fixed by the query's CASTs.
/// </summary>
public sealed class EisJobRow
{
    public string? JobCode { get; set; }
    public string? JobGroup { get; set; }

    /// <summary>Started on or before today and not yet ended (usp_eis_isInTitleList's test).</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Annual rate above zero; the legacy procedures' "not WOS".</summary>
    public bool IsPaid { get; set; }

    /// <summary>Annual rate of zero; the legacy procedures' "WOS" (without salary).</summary>
    public bool IsWithoutSalary { get; set; }

    /// <summary>EFF_DATE_ACTIVE = 1, the test usp_eis_isEmeritus and usp_eis_isRecall used.</summary>
    public bool IsActive { get; set; }

    public string? Description { get; set; }
}

/// <summary>A compensation rate code on one of the employee's active jobs (UCOFF1, UCABVE...).</summary>
public sealed class EisRateCodeRow
{
    public string? RateCode { get; set; }
}

/// <summary>A manual category set for the employee in the flags table.</summary>
public sealed class EisFlagRow
{
    public int Code { get; set; }
}

/// <summary>The staff and faculty programs, which decide the MSP and Senate categories.</summary>
public sealed record EisPrograms(string? StaffProgram, string? FacultyProgram);

/// <summary>usp_eis_getPersonMIVDegrees, read as text because the procedure's types can't be described.</summary>
public sealed record EisMivDegreeRow(
    string? Degree, string? StartDate, string? EndDate, string? Institution, string? Location, string? Field);

/// <summary>
/// A MyInfoVault text entry (membership, honor, board or focus area), usually HTML, with the
/// year or year span when the procedure returns one.
/// </summary>
public sealed record EisMivTextRow(string? Year, string? Text);

/// <summary>
/// Everything the header procedures return for one employee, before any interpretation.
/// Null <see cref="Name"/> means the employee is unknown.
/// </summary>
public sealed record EisHeaderData
{
    public string? Name { get; init; }
    public string? DateOfBirth { get; init; }
    public int? Age { get; init; }
    public string? Gender { get; init; }
    public string? Ethnicity { get; init; }
    public DateTime? HireDate { get; init; }
    public string? EmploymentStatus { get; init; }
    public string? Citizenship { get; init; }
    public string? Visa { get; init; }
    public string? PrimaryAffiliation { get; init; }
    public string? PrimaryTitle { get; init; }
    public string? JobGroup { get; init; }
    public string? HomeDepartment { get; init; }
    public string? AlternateDepartment { get; init; }
    public string? BargainingUnit { get; init; }
    public string? LaborRelationsUnit { get; init; }
    public int? StaffCount { get; init; }
    public string? StaffProgram { get; init; }
    public string? StaffStatus { get; init; }
    public int? FacultyCount { get; init; }
    public string? FacultyProgram { get; init; }
    public string? LadderRank { get; init; }
    public decimal? ServiceCredit { get; init; }
    public decimal? VacationBalance { get; init; }
    public decimal? SickBalance { get; init; }
    public decimal? PtoBalance { get; init; }
}
