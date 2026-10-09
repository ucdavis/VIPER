using System.Globalization;
using System.Text.RegularExpressions;
using Viper.Areas.Personnel.Models.Eis;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.Personnel.Services;

/// <summary>
/// The Employee Information System: who the current user may look up, and each page's data.
/// </summary>
public interface IEisService
{
    /// <summary>The people the current user may look up, in the procedure's order.</summary>
    Task<IReadOnlyList<EisPersonOption>> GetPeopleAsync(CancellationToken ct);

    /// <summary>
    /// Whether the current user may view this employee. Holders of <see cref="EisPermissions.Department"/>
    /// may view only the people their units pay; other EIS users may view anyone.
    /// </summary>
    Task<bool> CanViewAsync(string employeeId, CancellationToken ct);

    /// <summary>The header and summary details, or null when the employee is unknown.</summary>
    Task<EisPersonHeader?> GetHeaderAsync(string employeeId, CancellationToken ct);

    Task<EisAppointments> GetAppointmentsAsync(string employeeId, CancellationToken ct);

    Task<EisHistory> GetHistoryAsync(string employeeId, CancellationToken ct);

    Task<EisAddress> GetAddressAsync(string employeeId, CancellationToken ct);

    /// <summary>The employee's campus mail ID, which names their ID card photo, or null.</summary>
    Task<string?> GetMailIdAsync(string employeeId, CancellationToken ct);

    /// <summary>Every appointment category, with whether it applies and whether the user may edit the manual ones.</summary>
    Task<EisAppointmentCategories> GetAppointmentCategoriesAsync(string employeeId, CancellationToken ct);

    /// <summary>
    /// Sets or clears a manual category for the current academic year and returns the updated
    /// categories, or null when the current user lacks <see cref="EisPermissions.Admin"/>.
    /// Setting a category that is already set, or clearing one that isn't, changes nothing.
    /// </summary>
    Task<EisAppointmentCategories?> SetFlagAsync(string employeeId, int code, bool isSet, CancellationToken ct);

    /// <summary>The employee's degrees, boards, memberships, focus areas and honors from MyInfoVault.</summary>
    Task<EisAcademics> GetAcademicsAsync(string employeeId, CancellationToken ct);
}

public partial class EisService : IEisService
{
    /// <summary>Earnings paid each pay period rather than once; the annual amount is twelve times the pay.</summary>
    public const string PeriodicPay = "P";

    private const string EmailDomain = "@ucdavis.edu";
    private const decimal MonthsPerYear = 12m;
    private const decimal Hundred = 100m;

    private readonly IEisDataService _data;
    private readonly IEisDirectoryService _directory;
    private readonly IUserHelper _userHelper;
    private readonly RAPSContext _rapsContext;
    private readonly TimeProvider _clock;

    public EisService(
        IEisDataService data,
        IEisDirectoryService directory,
        IUserHelper userHelper,
        RAPSContext rapsContext,
        TimeProvider clock)
    {
        _data = data;
        _directory = directory;
        _userHelper = userHelper;
        _rapsContext = rapsContext;
        _clock = clock;
    }

    /// <summary>Whether <paramref name="code"/> is one of the manual categories (dvtFlags).</summary>
    public static bool IsFlagCode(int code)
    {
        return EisCategoryRules.Flags.ContainsKey(code);
    }

    /// <summary>
    /// Employee IDs are digits only, at most 11 long (the procedures' parameter size). Anything
    /// else is rejected before it reaches the database.
    /// </summary>
    public static bool IsValidEmployeeId(string? employeeId)
    {
        return employeeId is not null && EmployeeIdPattern().IsMatch(employeeId);
    }

    public async Task<IReadOnlyList<EisPersonOption>> GetPeopleAsync(CancellationToken ct)
    {
        IReadOnlyList<EisPersonRow>? rows = await GetVisiblePeopleAsync(ct);
        return rows is null
            ? []
            : [.. rows.Select(row => new EisPersonOption(row.EmployeeId.Trim(), row.Name.Trim()))];
    }

    public async Task<bool> CanViewAsync(string employeeId, CancellationToken ct)
    {
        if (!IsValidEmployeeId(employeeId))
        {
            return false;
        }

        AaudUser? user = _userHelper.GetCurrentUser();
        if (user is null)
        {
            return false;
        }

        if (!IsDepartmentOnly(user))
        {
            return HasPermission(user, EisPermissions.View);
        }

        IReadOnlyList<EisPersonRow> unit = await GetUnitPeopleAsync(user, ct);
        return unit.Any(row => string.Equals(row.EmployeeId.Trim(), employeeId, StringComparison.Ordinal));
    }

    public async Task<EisPersonHeader?> GetHeaderAsync(string employeeId, CancellationToken ct)
    {
        EisHeaderData data = await _data.GetHeaderAsync(employeeId, ct);
        if (Clean(data.Name) is not { } name)
        {
            return null;
        }

        EisPersonIds? ids = await _data.GetPersonIdsAsync(employeeId, ct);
        return new EisPersonHeader
        {
            EmployeeId = employeeId,
            Name = name,
            Email = Clean(ids?.MailId) is { } mailId ? mailId + EmailDomain : null,
            DateOfBirth = Clean(data.DateOfBirth),
            Age = data.Age,
            Gender = Clean(data.Gender),
            Ethnicity = Clean(data.Ethnicity),
            HireDate = ToDate(data.HireDate),
            EmploymentStatus = Clean(data.EmploymentStatus),
            Citizenship = Clean(data.Citizenship),
            Visa = Clean(data.Visa),
            PrimaryAffiliation = Clean(data.PrimaryAffiliation),
            PrimaryTitle = Clean(data.PrimaryTitle),
            JobGroup = Clean(data.JobGroup),
            HomeDepartment = Clean(data.HomeDepartment),
            AlternateDepartment = Clean(data.AlternateDepartment),
            BargainingUnit = Clean(data.BargainingUnit),
            LaborRelationsUnit = Clean(data.LaborRelationsUnit),
            IsStaff = data.StaffCount > 0,
            StaffProgram = Clean(data.StaffProgram),
            StaffStatus = Clean(data.StaffStatus),
            IsFaculty = data.FacultyCount > 0,
            FacultyProgram = Clean(data.FacultyProgram),
            LadderRank = Clean(data.LadderRank),
            ServiceCreditMonths = data.ServiceCredit ?? 0m,
            VacationHours = data.VacationBalance,
            SickHours = data.SickBalance,
            PtoHours = data.PtoBalance,
        };
    }

    public async Task<EisAppointments> GetAppointmentsAsync(string employeeId, CancellationToken ct)
    {
        var appointments = new List<EisAppointment>();
        foreach (EisAppointmentRow row in await _data.GetAppointmentsAsync(employeeId, ct))
        {
            // One query per appointment, as in the legacy page; people hold only a few.
            IReadOnlyList<EisDistributionRow> distributions = Clean(row.Number) is { } number
                ? await _data.GetDistributionsAsync(employeeId, number, ct)
                : [];
            appointments.Add(ToAppointment(row, distributions));
        }

        List<EisStipend> stipends = [.. (await _data.GetStipendsAsync(employeeId, ct)).Select(ToStipend)];
        decimal total = appointments.Sum(appointment => appointment.Total ?? 0m)
            + stipends.Sum(stipend => stipend.AnnualAmount);
        return new EisAppointments(appointments, stipends, total);
    }

    public async Task<EisHistory> GetHistoryAsync(string employeeId, CancellationToken ct)
    {
        IReadOnlyList<EisHistoryRow> history = await _data.GetHistoryAsync(employeeId, ct);
        IReadOnlyList<EisLeaveRow> leaves = await _data.GetLeavesAsync(employeeId, ct);

        // Records from the retired PPS system are keyed by the person's old PPS ID.
        IReadOnlyList<EisPpsHistoryRow> ppsHistory = [];
        IReadOnlyList<EisLeaveRow> ppsLeaves = [];
        if (Clean((await _data.GetPersonIdsAsync(employeeId, ct))?.PpsId) is { } ppsId)
        {
            ppsHistory = await _data.GetPpsHistoryAsync(ppsId, ct);
            ppsLeaves = await _data.GetPpsLeavesAsync(ppsId, ct);
        }

        return new EisHistory(
            [.. history.Select(ToHistoryEntry)],
            [.. ppsHistory.Select(ToHistoryEntry)],
            [.. leaves.Select(ToLeave)],
            [.. ppsLeaves.Select(ToLeave)]);
    }

    public async Task<EisAddress> GetAddressAsync(string employeeId, CancellationToken ct)
    {
        EisPermanentAddressRow? address = await _data.GetPermanentAddressAsync(employeeId, ct);
        EisHomePhoneRow? phone = await _data.GetHomePhoneAsync(employeeId, ct);
        EisPersonIds? ids = await _data.GetPersonIdsAsync(employeeId, ct);
        IReadOnlyList<EisCampusListing>? listings = _directory.GetCampusListings(employeeId, Clean(ids?.MothraId));

        return new EisAddress(
            address is null ? null : ToPermanentAddress(address),
            phone is null ? null : ToHomePhone(phone),
            listings ?? [],
            listings is not null);
    }

    public async Task<string?> GetMailIdAsync(string employeeId, CancellationToken ct)
    {
        return Clean((await _data.GetPersonIdsAsync(employeeId, ct))?.MailId);
    }

    public async Task<EisAppointmentCategories> GetAppointmentCategoriesAsync(string employeeId, CancellationToken ct)
    {
        string academicYear = CurrentAcademicYear();
        IReadOnlyList<EisJobRow> jobs = await _data.GetJobsAsync(employeeId, ct);
        IReadOnlyList<string> rateCodes = await _data.GetRateCodesAsync(employeeId, ct);
        IReadOnlyList<int> flags = await _data.GetFlagCodesAsync(employeeId, academicYear, ct);
        EisPrograms programs = await _data.GetProgramsAsync(employeeId, ct);
        return new EisAppointmentCategories(
            EisCategoryRules.Evaluate(jobs, rateCodes, flags, programs),
            academicYear,
            CanEditFlags());
    }

    public async Task<EisAppointmentCategories?> SetFlagAsync(string employeeId, int code, bool isSet, CancellationToken ct)
    {
        if (!CanEditFlags())
        {
            return null;
        }

        // usp_eis_putPersonModFlags inserts without checking for a duplicate, so only call it
        // when the category actually changes.
        IReadOnlyList<int> current = await _data.GetFlagCodesAsync(employeeId, CurrentAcademicYear(), ct);
        if (current.Contains(code) != isSet)
        {
            await _data.SetFlagAsync(employeeId, code, isSet, ct);
        }

        return await GetAppointmentCategoriesAsync(employeeId, ct);
    }

    public async Task<EisAcademics> GetAcademicsAsync(string employeeId, CancellationToken ct)
    {
        // The legacy page treated a missing or negative MyInfoVault ID as "no account".
        if ((await _data.GetPersonIdsAsync(employeeId, ct))?.MivId is not { } mivId || mivId <= 0)
        {
            return new EisAcademics(false, [], [], [], [], [], []);
        }

        IReadOnlyList<EisMivDegreeRow> degrees = await _data.GetMivDegreesAsync(mivId, ct);
        IReadOnlyList<EisMivTextRow> boards = await _data.GetMivBoardsAsync(mivId, ct);
        IReadOnlyList<EisMivTextRow> memberships = await _data.GetMivMembershipsAsync(mivId, ct);
        IReadOnlyList<EisMivTextRow> research = await _data.GetMivResearchFocusAsync(mivId, ct);
        IReadOnlyList<EisMivTextRow> specialty = await _data.GetMivSpecialtyFocusAsync(mivId, ct);
        IReadOnlyList<EisMivTextRow> honors = await _data.GetMivHonorsAsync(mivId, ct);
        return new EisAcademics(
            true,
            [.. degrees.Select(ToDegree)],
            ToDatedItems(boards),
            [.. memberships.Select(row => EisHtmlText.ToText(row.Text)).OfType<string>()],
            [.. research.SelectMany(row => EisHtmlText.ToItems(row.Text))],
            [.. specialty.SelectMany(row => EisHtmlText.ToItems(row.Text))],
            ToDatedItems(honors));
    }

    /// <summary>
    /// The appointment and its distributions. Each distribution's amount is the annual amount
    /// times its percent; the appointment total is its pay rate, as the legacy page showed it.
    /// </summary>
    internal static EisAppointment ToAppointment(EisAppointmentRow row, IReadOnlyList<EisDistributionRow> distributions)
    {
        return new EisAppointment(
            Clean(row.Number),
            row.Title.Trim(),
            row.TitleCode.Trim(),
            Clean(row.Grade),
            row.ExemptStatus.Trim(),
            row.Department.Trim(),
            Clean(row.BeginDate),
            Clean(row.EndDate),
            [.. distributions.Select(ToDistribution)],
            distributions.Count > 0 ? distributions[0].PayRate : null);
    }

    internal static EisDistribution ToDistribution(EisDistributionRow row)
    {
        return new EisDistribution(
            row.Number?.ToString("0.###", CultureInfo.InvariantCulture),
            DateOnly.FromDateTime(row.BeginDate),
            ToDate(row.EndDate),
            row.Percent,
            Clean(row.Account),
            row.Step.ToString("0", CultureInfo.InvariantCulture),
            Clean(row.DosCode),
            row.AnnualAmount * (row.Percent ?? 0m) / Hundred);
    }

    internal static EisStipend ToStipend(EisStipendRow row)
    {
        decimal annual = string.Equals(row.PayFrequency.Trim(), PeriodicPay, StringComparison.OrdinalIgnoreCase)
            ? row.Pay * MonthsPerYear
            : row.Pay;
        return new EisStipend(
            DateOnly.FromDateTime(row.EffectiveDate), ToDate(row.EndDate), row.EarningsCode.Trim(), annual);
    }

    /// <summary>A MyInfoVault degree; the legacy page showed its end date as the year.</summary>
    internal static EisDegree ToDegree(EisMivDegreeRow row)
    {
        return new EisDegree(
            EisHtmlText.ToText(row.EndDate),
            EisHtmlText.ToText(row.Degree),
            EisHtmlText.ToText(row.Institution),
            EisHtmlText.ToText(row.Location),
            EisHtmlText.ToText(row.Field));
    }

    /// <summary>Dated MyInfoVault entries as plain text, leaving out entries with no text.</summary>
    internal static IReadOnlyList<EisDatedItem> ToDatedItems(IEnumerable<EisMivTextRow> rows)
    {
        return [.. rows
            .Select(row => EisHtmlText.ToText(row.Text) is { } text ? new EisDatedItem(EisHtmlText.ToText(row.Year), text) : null)
            .OfType<EisDatedItem>()];
    }

    private static EisHistoryEntry ToHistoryEntry(EisHistoryRow row)
    {
        return new EisHistoryEntry(
            DateOnly.FromDateTime(row.ActionDate),
            Clean(row.Title),
            Clean(row.TitleCode),
            Clean(row.Department),
            DateOnly.FromDateTime(row.BeginDate),
            ToDate(row.EndDate),
            row.Step.ToString("0", CultureInfo.InvariantCulture),
            row.Percent,
            row.PayRate,
            Clean(row.Comment));
    }

    private static EisHistoryEntry ToHistoryEntry(EisPpsHistoryRow row)
    {
        return new EisHistoryEntry(
            ToDate(row.ActionDate),
            Clean(row.Title),
            Clean(row.TitleCode),
            Clean(row.Department),
            ToDate(row.BeginDate),
            ToDate(row.EndDate),
            Clean(row.Step),
            row.Percent,
            row.PayRate,
            Clean(row.Comment));
    }

    private static EisLeave ToLeave(EisLeaveRow row)
    {
        return new EisLeave(ToDate(row.BeginDate), ToDate(row.ReturnDate), Clean(row.Description));
    }

    private static EisPermanentAddress ToPermanentAddress(EisPermanentAddressRow row)
    {
        return new EisPermanentAddress(
            row.Line1.Trim(),
            Clean(row.Line2),
            row.City.Trim(),
            row.State.Trim(),
            row.Zip.Trim(),
            row.ReleaseCampus.Trim(),
            row.ReleaseOrganization.Trim());
    }

    private static EisHomePhone ToHomePhone(EisHomePhoneRow row)
    {
        return new EisHomePhone(
            row.Phone.Trim(),
            row.ReleaseCampus?.ToString(CultureInfo.InvariantCulture),
            row.ReleaseOrganization?.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<IReadOnlyList<EisPersonRow>?> GetVisiblePeopleAsync(CancellationToken ct)
    {
        AaudUser? user = _userHelper.GetCurrentUser();
        if (user is null)
        {
            return null;
        }

        if (IsDepartmentOnly(user))
        {
            return await GetUnitPeopleAsync(user, ct);
        }

        return HasPermission(user, EisPermissions.View) ? await _data.GetPeopleAsync(ct) : null;
    }

    private async Task<IReadOnlyList<EisPersonRow>> GetUnitPeopleAsync(AaudUser user, CancellationToken ct)
    {
        return string.IsNullOrWhiteSpace(user.LoginId) ? [] : await _data.GetUnitPeopleAsync(user.LoginId, ct);
    }

    private string CurrentAcademicYear()
    {
        return EisCategoryRules.AcademicYear(DateOnly.FromDateTime(_clock.GetLocalNow().DateTime));
    }

    private bool CanEditFlags()
    {
        return _userHelper.GetCurrentUser() is { } user && HasPermission(user, EisPermissions.Admin);
    }

    private bool HasPermission(AaudUser user, string permission)
    {
        return _userHelper.HasPermission(_rapsContext, user, permission);
    }

    /// <summary>
    /// Whether the user holds <see cref="EisPermissions.Department"/> itself. It narrows access, so it
    /// is looked up by name rather than through <see cref="IUserHelper.HasPermission"/>, which grants
    /// every permission to superusers and would restrict them to their own units.
    /// </summary>
    private bool IsDepartmentOnly(AaudUser user)
    {
        return _userHelper.GetAllPermissions(_rapsContext, user)
            .Any(p => string.Equals(p.Permission, EisPermissions.Department, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>UCPath text is often padded with spaces; blank text becomes null.</summary>
    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateOnly? ToDate(DateTime? value)
    {
        return value is { } date ? DateOnly.FromDateTime(date) : null;
    }

    [GeneratedRegex("^[0-9]{1,11}$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
    private static partial Regex EmployeeIdPattern();
}
