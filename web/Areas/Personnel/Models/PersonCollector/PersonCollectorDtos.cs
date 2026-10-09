namespace Viper.Areas.Personnel.Models.PersonCollector;

/// <summary>
/// What to collect. Lists hold option keys from <see cref="PersonCollectorOptions"/>. The
/// department filter applies to every staff and faculty section; with no departments chosen,
/// those sections cover the whole school, as in the legacy page.
/// </summary>
public sealed record PersonCollectorRequest
{
    public IReadOnlyList<string> Departments { get; init; } = [];

    /// <summary>Also list everyone in the chosen departments (needs at least one department).</summary>
    public bool FullDepartmentList { get; init; }

    public IReadOnlyList<string> SenateGroups { get; init; } = [];
    public bool SenateEmeriti { get; init; }
    public IReadOnlyList<string> FederationGroups { get; init; } = [];
    public bool StaffMsp { get; init; }
    public bool StaffPss { get; init; }
    public bool StaffVeterinarians { get; init; }
    public IReadOnlyList<string> StudentClasses { get; init; } = [];
    public bool StudentMpvm { get; init; }
}

/// <summary>A checkbox choice as the form shows it.</summary>
public sealed record PersonCollectorChoice(string Key, string Label);

/// <summary>The form's choices and which optional columns this user will see.</summary>
public sealed record PersonCollectorForm(
    IReadOnlyList<PersonCollectorChoice> Departments,
    IReadOnlyList<PersonCollectorChoice> SenateGroups,
    IReadOnlyList<PersonCollectorChoice> FederationGroups,
    IReadOnlyList<PersonCollectorChoice> StudentClasses,
    bool ShowLoginIds,
    bool ShowMoreIds);

/// <summary>
/// One person in a result section. The ID fields are null unless the user holds the matching
/// permission, so they never leave the server for users who may not see them.
/// </summary>
public sealed record PersonCollectorPerson(
    string? Name,
    string? Email,
    string? LoginId,
    string? EmployeeId,
    string? MothraId,
    string? MailId,
    string? Pidm,
    string? BannerId);

/// <summary>One list on the results page, such as "Senate Faculty".</summary>
public sealed record PersonCollectorSection(string Key, string Title, IReadOnlyList<PersonCollectorPerson> People);

/// <summary>The results: one section per group chosen, in the legacy order.</summary>
public sealed record PersonCollectorResult(
    IReadOnlyList<PersonCollectorSection> Sections,
    bool ShowLoginIds,
    bool ShowMoreIds);
