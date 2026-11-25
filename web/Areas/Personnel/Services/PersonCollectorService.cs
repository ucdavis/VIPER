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
        return new PersonCollectorForm
        {
            Departments = Choices(PersonCollectorOptions.Departments),
            SenateGroups = Choices(PersonCollectorOptions.SenateGroups),
            FederationGroups = Choices(PersonCollectorOptions.FederationGroups),
            StudentClasses = Choices(PersonCollectorOptions.StudentClasses),
            ShowLoginIds = showLoginIds,
            ShowMoreIds = showMoreIds,
        };
    }

    public async Task<PersonCollectorResult> CollectAsync(PersonCollectorRequest request, CancellationToken ct)
    {
        PersonCollectorPlan plan = PersonCollectorPlan.Create(request);
        (bool showLoginIds, bool showMoreIds) = Columns();
        var sections = new List<PersonCollectorSection>();
        PersonCollectorSection Section(string key, IReadOnlyList<PersonCollectorRow> rows) =>
            new()
            {
                Key = key,
                Title = PersonCollectorPlan.Titles[key],
                People = [.. rows.Select(row => Limit(PersonCollectorMapper.ToPerson(row), showLoginIds, showMoreIds))],
            };

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

        return new PersonCollectorResult
        {
            Sections = sections,
            ShowLoginIds = showLoginIds,
            ShowMoreIds = showMoreIds,
        };
    }

    /// <summary>The person with the columns this user may not see removed.</summary>
    internal static PersonCollectorPerson Limit(PersonCollectorPerson person, bool showLoginIds, bool showMoreIds)
    {
        return new PersonCollectorPerson
        {
            Name = person.Name,
            Email = person.Email,
            LoginId = showLoginIds ? person.LoginId : null,
            EmployeeId = showMoreIds ? person.EmployeeId : null,
            MothraId = showMoreIds ? person.MothraId : null,
            MailId = showMoreIds ? person.MailId : null,
            Pidm = showMoreIds ? person.Pidm : null,
            BannerId = showMoreIds ? person.BannerId : null,
        };
    }

    private static IReadOnlyList<PersonCollectorChoice> Choices(IReadOnlyList<PersonCollectorOption> options)
    {
        return [.. options.Select(option => new PersonCollectorChoice { Key = option.Key, Label = option.Label })];
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
}
