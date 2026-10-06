namespace Viper.Areas.Personnel.Models.Eis;

/// <summary>One entry in the EIS person picker.</summary>
public sealed record EisPersonOption(string EmployeeId, string Name);

/// <summary>
/// The person header shown above every EIS page, plus the summary-only details
/// (demographics, service credit and leave balances).
/// </summary>
public sealed record EisPersonHeader
{
    public required string EmployeeId { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public string? DateOfBirth { get; init; }
    public int? Age { get; init; }
    public string? Gender { get; init; }
    public string? Ethnicity { get; init; }
    public DateOnly? HireDate { get; init; }
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
    public bool IsStaff { get; init; }
    public string? StaffProgram { get; init; }
    public string? StaffStatus { get; init; }
    public bool IsFaculty { get; init; }
    public string? FacultyProgram { get; init; }
    public string? LadderRank { get; init; }

    /// <summary>Service credit in months; zero when the procedure has none, as in the legacy page.</summary>
    public decimal ServiceCreditMonths { get; init; }

    public decimal? VacationHours { get; init; }
    public decimal? SickHours { get; init; }
    public decimal? PtoHours { get; init; }
}

/// <summary>One distribution line of an appointment.</summary>
/// <param name="Amount">The annual amount times the distribution percent; negative for reductions.</param>
public sealed record EisDistribution(
    string? Number,
    DateOnly BeginDate,
    DateOnly? EndDate,
    decimal? Percent,
    string? Account,
    string Step,
    string? DosCode,
    decimal Amount);

/// <summary>
/// One appointment with its distributions. <see cref="Total"/> is the appointment's pay rate, as
/// the legacy page showed it, and is null when the appointment has no distributions.
/// </summary>
public sealed record EisAppointment(
    string? Number,
    string Title,
    string TitleCode,
    string? Grade,
    string ExemptStatus,
    string Department,
    string? BeginDate,
    string? EndDate,
    IReadOnlyList<EisDistribution> Distributions,
    decimal? Total);

/// <summary>A stipend, with its pay converted to an annual amount.</summary>
public sealed record EisStipend(DateOnly EffectiveDate, DateOnly? EndDate, string EarningsCode, decimal AnnualAmount);

/// <summary>The Appointments page: appointments, stipends and the annual total of both.</summary>
public sealed record EisAppointments(
    IReadOnlyList<EisAppointment> Appointments,
    IReadOnlyList<EisStipend> Stipends,
    decimal TotalAnnual);

/// <summary>One appointment history action, from UCPath or the retired PPS system.</summary>
public sealed record EisHistoryEntry(
    DateOnly? ActionDate,
    string? Title,
    string? TitleCode,
    string? Department,
    DateOnly? BeginDate,
    DateOnly? EndDate,
    string? Step,
    decimal? Percent,
    decimal? PayRate,
    string? Comment);

/// <summary>One leave of absence.</summary>
public sealed record EisLeave(DateOnly? BeginDate, DateOnly? ReturnDate, string? Description);

/// <summary>
/// The Appointment History page. The PPS lists are empty when the employee has no PPS ID.
/// </summary>
public sealed record EisHistory(
    IReadOnlyList<EisHistoryEntry> Appointments,
    IReadOnlyList<EisHistoryEntry> PpsAppointments,
    IReadOnlyList<EisLeave> Leaves,
    IReadOnlyList<EisLeave> PpsLeaves);

/// <summary>
/// The permanent address from UCPath. The release flags say whether the employee allows the
/// campus directory and employee organizations to publish it; "N" means no.
/// </summary>
public sealed record EisPermanentAddress(
    string Line1,
    string? Line2,
    string City,
    string State,
    string Zip,
    string ReleaseCampus,
    string ReleaseOrganization);

/// <summary>
/// The home phone from UCPath. Its release flags come back as numbers, which the legacy page
/// never interpreted, so they are passed through as text.
/// </summary>
public sealed record EisHomePhone(string Phone, string? ReleaseCampus, string? ReleaseOrganization);

/// <summary>
/// One campus directory listing. The primary listing is the employee's own entry; additional
/// listings that are not public (ucdPublish other than "W") are flagged.
/// </summary>
public sealed record EisCampusListing(
    bool IsPrimary,
    bool IsPublic,
    string? Title,
    string? Department,
    string? Address,
    string? Phone);

/// <summary>
/// The Address page. <see cref="CampusDirectoryAvailable"/> is false when the campus directory
/// could not be reached, so the page can say so rather than show no listings.
/// </summary>
public sealed record EisAddress(
    EisPermanentAddress? Permanent,
    EisHomePhone? HomePhone,
    IReadOnlyList<EisCampusListing> CampusListings,
    bool CampusDirectoryAvailable);

/// <summary>
/// One row of the Appointment Category page. <see cref="FlagCode"/> is set for the manual
/// categories (dvtFlags), which EIS admins set and clear for the current academic year.
/// </summary>
public sealed record EisCategory(string Label, bool Applies, int? FlagCode);

/// <summary>The Appointment Category page.</summary>
/// <param name="AcademicYear">The academic year the manual categories belong to, such as "2026-2027".</param>
/// <param name="CanEditFlags">Whether the current user may set and clear the manual categories.</param>
public sealed record EisAppointmentCategories(
    IReadOnlyList<EisCategory> Categories,
    string AcademicYear,
    bool CanEditFlags);

/// <summary>One degree from MyInfoVault. The legacy page showed the end date as the year.</summary>
public sealed record EisDegree(string? Year, string? Degree, string? Institution, string? Location, string? Field);

/// <summary>A dated MyInfoVault entry (a board or license, or an award or honor), as plain text.</summary>
public sealed record EisDatedItem(string? Year, string Text);

/// <summary>
/// The Awards &amp; Degrees page, from MyInfoVault. MyInfoVault stores most of these as HTML;
/// they are returned as plain text. <see cref="HasMivAccount"/> is false when the employee has
/// no MyInfoVault account, and every list is then empty.
/// </summary>
public sealed record EisAcademics(
    bool HasMivAccount,
    IReadOnlyList<EisDegree> Degrees,
    IReadOnlyList<EisDatedItem> Boards,
    IReadOnlyList<string> Memberships,
    IReadOnlyList<string> ResearchFocus,
    IReadOnlyList<string> SpecialtyFocus,
    IReadOnlyList<EisDatedItem> Honors);
