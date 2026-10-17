using Viper.Areas.Personnel.Models.PersonCollector;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;

namespace Viper.Areas.Personnel.Services;

/// <summary>
/// The Person Collector: lists of faculty, staff and students chosen by department and group,
/// with the ID columns each user is allowed to see.
/// </summary>
public interface IPersonCollectorService
{
    /// <summary>The form's choices and which optional columns the current user will see.</summary>
    PersonCollectorForm GetForm();

    /// <summary>One section per group chosen, in the legacy order. Validate the keys first.</summary>
    Task<PersonCollectorResult> CollectAsync(PersonCollectorRequest request, CancellationToken ct);
}

public class PersonCollectorService : IPersonCollectorService
{
    private readonly IPersonCollectorDataService _data;
    private readonly IUserHelper _userHelper;
    private readonly RAPSContext _rapsContext;

    public PersonCollectorService(IPersonCollectorDataService data, IUserHelper userHelper, RAPSContext rapsContext)
    {
        _data = data;
        _userHelper = userHelper;
        _rapsContext = rapsContext;
    }

    public PersonCollectorForm GetForm()
    {
        (bool showLoginIds, bool showMoreIds) = Columns();
        return new PersonCollectorForm(
            Choices(PersonCollectorOptions.Departments),
            Choices(PersonCollectorOptions.SenateGroups),
            Choices(PersonCollectorOptions.FederationGroups),
            Choices(PersonCollectorOptions.StudentClasses),
            showLoginIds,
            showMoreIds);
    }

    public async Task<PersonCollectorResult> CollectAsync(PersonCollectorRequest request, CancellationToken ct)
    {
        PersonCollectorPlan plan = PersonCollectorPlan.Create(request);
        (bool showLoginIds, bool showMoreIds) = Columns();
        var sections = new List<PersonCollectorSection>();
        PersonCollectorSection Section(string key, IReadOnlyList<PersonCollectorRow> rows) =>
            new(key, PersonCollectorPlan.Titles[key], [.. rows.Select(row => Limit(ToPerson(row), showLoginIds, showMoreIds))]);

        // One procedure call per section, one after another: a DbContext can't run queries in parallel.
        if (plan.FullDepartmentList is { } departments)
        {
            sections.Add(Section(PersonCollectorPlan.DepartmentKey, await _data.GetDepartmentAsync(departments, ct)));
        }

        if (plan.Senate is { } senate)
        {
            sections.Add(Section(PersonCollectorPlan.SenateKey, await _data.GetFacultyAsync(senate, ct)));
        }

        if (plan.Federation is { } federation)
        {
            sections.Add(Section(PersonCollectorPlan.FederationKey, await _data.GetFacultyAsync(federation, ct)));
        }

        if (plan.Staff is { } staff)
        {
            sections.Add(Section(PersonCollectorPlan.StaffKey, await _data.GetStaffAsync(staff, ct)));
        }

        if (plan.Students is { } students)
        {
            PersonCollectorTerms terms = await _data.GetCurrentTermsAsync(ct);
            sections.Add(Section(PersonCollectorPlan.StudentsKey, await _data.GetStudentsAsync(students, terms, ct)));
        }

        return new PersonCollectorResult(sections, showLoginIds, showMoreIds);
    }

    /// <summary>The person with the columns this user may not see removed.</summary>
    internal static PersonCollectorPerson Limit(PersonCollectorPerson person, bool showLoginIds, bool showMoreIds)
    {
        return new PersonCollectorPerson(
            person.Name,
            person.Email,
            showLoginIds ? person.LoginId : null,
            showMoreIds ? person.EmployeeId : null,
            showMoreIds ? person.MothraId : null,
            showMoreIds ? person.MailId : null,
            showMoreIds ? person.Pidm : null,
            showMoreIds ? person.BannerId : null);
    }

    private static PersonCollectorPerson ToPerson(PersonCollectorRow row)
    {
        return new PersonCollectorPerson(
            Clean(row.Name),
            Clean(row.Email),
            Clean(row.LoginId),
            Clean(row.EmployeeId),
            Clean(row.MothraId),
            Clean(row.MailId),
            Clean(row.Pidm),
            Clean(row.BannerId));
    }

    private static IReadOnlyList<PersonCollectorChoice> Choices(IReadOnlyList<PersonCollectorOption> options)
    {
        return [.. options.Select(option => new PersonCollectorChoice(option.Key, option.Label))];
    }

    private (bool ShowLoginIds, bool ShowMoreIds) Columns()
    {
        AaudUser? user = _userHelper.GetCurrentUser();
        return user is null
            ? (false, false)
            : (HasPermission(user, PersonCollectorPermissions.LoginIds), HasPermission(user, PersonCollectorPermissions.MoreIds));
    }

    private bool HasPermission(AaudUser user, string permission)
    {
        return _userHelper.HasPermission(_rapsContext, user, permission);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
