using System.Text.Json.Serialization;

namespace Viper.Areas.Personnel.Models.PersonCollector;

/// <summary>
/// What to collect. Lists hold option keys from <see cref="PersonCollectorOptions"/>. The
/// department filter applies to every staff and faculty section; with no departments chosen,
/// those sections cover the whole school, as in the legacy page. The page always sends every
/// flag, so a request missing one is rejected rather than read as unchecked.
/// </summary>
public sealed record PersonCollectorRequest
{
    public IReadOnlyList<string> Departments { get; init; } = [];

    /// <summary>Also list everyone in the chosen departments (needs at least one department).</summary>
    [JsonRequired]
    public bool FullDepartmentList { get; init; }

    public IReadOnlyList<string> SenateGroups { get; init; } = [];
    [JsonRequired]
    public bool SenateEmeriti { get; init; }
    public IReadOnlyList<string> FederationGroups { get; init; } = [];
    [JsonRequired]
    public bool StaffMsp { get; init; }
    [JsonRequired]
    public bool StaffPss { get; init; }
    [JsonRequired]
    public bool StaffVeterinarians { get; init; }
    public IReadOnlyList<string> StudentClasses { get; init; } = [];
    [JsonRequired]
    public bool StudentMpvm { get; init; }
}

/// <summary>A checkbox choice as the form shows it.</summary>
public sealed record PersonCollectorChoice
{
    public required string Key { get; init; }
    public required string Label { get; init; }
}

/// <summary>The form's choices and which optional columns this user will see.</summary>
public sealed record PersonCollectorForm
{
    public required IReadOnlyList<PersonCollectorChoice> Departments { get; init; }
    public required IReadOnlyList<PersonCollectorChoice> SenateGroups { get; init; }
    public required IReadOnlyList<PersonCollectorChoice> FederationGroups { get; init; }
    public required IReadOnlyList<PersonCollectorChoice> StudentClasses { get; init; }
    public required bool ShowLoginIds { get; init; }
    public required bool ShowMoreIds { get; init; }
}

/// <summary>
/// One person in a result section. The ID fields are null unless the user holds the matching
/// permission, so they never leave the server for users who may not see them.
/// </summary>
public sealed record PersonCollectorPerson
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? LoginId { get; init; }
    public string? EmployeeId { get; init; }
    public string? MothraId { get; init; }
    public string? MailId { get; init; }
    public string? Pidm { get; init; }
    public string? BannerId { get; init; }
}

/// <summary>One list on the results page, such as "Senate Faculty".</summary>
public sealed record PersonCollectorSection
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required IReadOnlyList<PersonCollectorPerson> People { get; init; }
}

/// <summary>The results: one section per group chosen, in the legacy order.</summary>
public sealed record PersonCollectorResult
{
    public required IReadOnlyList<PersonCollectorSection> Sections { get; init; }
    public required bool ShowLoginIds { get; init; }
    public required bool ShowMoreIds { get; init; }
}
