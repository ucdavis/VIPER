namespace Viper.Areas.Personnel.Models.Eis;

/// <summary>One entry in the EIS person picker.</summary>
public sealed record EisPersonOption
{
    public required string EmployeeId { get; init; }
    public required string Name { get; init; }
}

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
public sealed record EisDistribution
{
    public string? Number { get; init; }
    public required DateOnly BeginDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public decimal? Percent { get; init; }
    public string? Account { get; init; }
    public required string Step { get; init; }
    public string? DosCode { get; init; }
    /// <summary>The annual amount times the distribution percent; negative for reductions.</summary>
    public required decimal Amount { get; init; }
}

/// <summary>
/// One appointment with its distributions. <see cref="Total"/> is the appointment's pay rate, as
/// the legacy page showed it, and is null when the appointment has no distributions.
/// </summary>
public sealed record EisAppointment
{
    public string? Number { get; init; }
    public required string Title { get; init; }
    public required string TitleCode { get; init; }
    public string? Grade { get; init; }
    public required string ExemptStatus { get; init; }
    public required string Department { get; init; }
    public string? BeginDate { get; init; }
    public string? EndDate { get; init; }
    public required IReadOnlyList<EisDistribution> Distributions { get; init; }
    public decimal? Total { get; init; }
}

/// <summary>A stipend, with its pay converted to an annual amount.</summary>
public sealed record EisStipend
{
    public required DateOnly EffectiveDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required string EarningsCode { get; init; }
    public required decimal AnnualAmount { get; init; }
}

/// <summary>The Appointments page: appointments, stipends and the annual total of both.</summary>
public sealed record EisAppointments
{
    public required IReadOnlyList<EisAppointment> Appointments { get; init; }
    public required IReadOnlyList<EisStipend> Stipends { get; init; }
    public required decimal TotalAnnual { get; init; }
}

/// <summary>One appointment history action, from UCPath or the retired PPS system.</summary>
public sealed record EisHistoryEntry
{
    public DateOnly? ActionDate { get; init; }
    public string? Title { get; init; }
    public string? TitleCode { get; init; }
    public string? Department { get; init; }
    public DateOnly? BeginDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Step { get; init; }
    public decimal? Percent { get; init; }
    public decimal? PayRate { get; init; }
    public string? Comment { get; init; }
}

/// <summary>One leave of absence.</summary>
public sealed record EisLeave
{
    public DateOnly? BeginDate { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// The Appointment History page. The PPS lists are empty when the employee has no PPS ID.
/// </summary>
public sealed record EisHistory
{
    public required IReadOnlyList<EisHistoryEntry> Appointments { get; init; }
    public required IReadOnlyList<EisHistoryEntry> PpsAppointments { get; init; }
    public required IReadOnlyList<EisLeave> Leaves { get; init; }
    public required IReadOnlyList<EisLeave> PpsLeaves { get; init; }
}

/// <summary>
/// The permanent address from UCPath. The release flags say whether the employee allows the
/// campus directory and employee organizations to publish it; "N" means no.
/// </summary>
public sealed record EisPermanentAddress
{
    public required string Line1 { get; init; }
    public string? Line2 { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string Zip { get; init; }
    public required string ReleaseCampus { get; init; }
    public required string ReleaseOrganization { get; init; }
}

/// <summary>
/// The home phone from UCPath. Its release flags come back as numbers, which the legacy page
/// never interpreted, so they are passed through as text.
/// </summary>
public sealed record EisHomePhone
{
    public required string Phone { get; init; }
    public string? ReleaseCampus { get; init; }
    public string? ReleaseOrganization { get; init; }
}

/// <summary>
/// One campus directory listing. The primary listing is the employee's own entry; additional
/// listings that are not public (ucdPublish other than "W") are flagged.
/// </summary>
public sealed record EisCampusListing
{
    public required bool IsPrimary { get; init; }
    public required bool IsPublic { get; init; }
    public string? Title { get; init; }
    public string? Department { get; init; }
    public string? Address { get; init; }
    public string? Phone { get; init; }
}

/// <summary>
/// The Address page. <see cref="CampusDirectoryAvailable"/> is false when the campus directory
/// could not be reached, so the page can say so rather than show no listings.
/// </summary>
public sealed record EisAddress
{
    public EisPermanentAddress? Permanent { get; init; }
    public EisHomePhone? HomePhone { get; init; }
    public required IReadOnlyList<EisCampusListing> CampusListings { get; init; }
    public required bool CampusDirectoryAvailable { get; init; }
}

/// <summary>
/// One row of the Appointment Category page. <see cref="FlagCode"/> is set for the manual
/// categories (dvtFlags), which EIS admins set and clear for the current academic year.
/// </summary>
public sealed record EisCategory
{
    public required string Label { get; init; }
    public required bool Applies { get; init; }
    public int? FlagCode { get; init; }
}

/// <summary>The Appointment Category page.</summary>
public sealed record EisAppointmentCategories
{
    public required IReadOnlyList<EisCategory> Categories { get; init; }
    /// <summary>The academic year the manual categories belong to, such as "2026-2027".</summary>
    public required string AcademicYear { get; init; }
    /// <summary>Whether the current user may set and clear the manual categories.</summary>
    public required bool CanEditFlags { get; init; }
}

/// <summary>One degree from MyInfoVault. The legacy page showed the end date as the year.</summary>
public sealed record EisDegree
{
    public string? Year { get; init; }
    public string? Degree { get; init; }
    public string? Institution { get; init; }
    public string? Location { get; init; }
    public string? Field { get; init; }
}

/// <summary>A dated MyInfoVault entry (a board or license, or an award or honor), as plain text.</summary>
public sealed record EisDatedItem
{
    public string? Year { get; init; }
    public required string Text { get; init; }
}

/// <summary>
/// The Awards &amp; Degrees page, from MyInfoVault. MyInfoVault stores most of these as HTML;
/// they are returned as plain text. <see cref="HasMivAccount"/> is false when the employee has
/// no MyInfoVault account, and every list is then empty.
/// </summary>
public sealed record EisAcademics
{
    public required bool HasMivAccount { get; init; }
    public required IReadOnlyList<EisDegree> Degrees { get; init; }
    public required IReadOnlyList<EisDatedItem> Boards { get; init; }
    public required IReadOnlyList<string> Memberships { get; init; }
    public required IReadOnlyList<string> ResearchFocus { get; init; }
    public required IReadOnlyList<string> SpecialtyFocus { get; init; }
    public required IReadOnlyList<EisDatedItem> Honors { get; init; }
}
