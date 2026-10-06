namespace Viper.Areas.Personnel.Models.PersonCollector;

/// <summary>Arguments for usp_get_personCollectorSenateFaculty, which serves both faculty sections.</summary>
/// <param name="DepartmentList">Comma-separated department IDs, or null for every SVM department.</param>
/// <param name="JobGroups">Comma-separated job groups, or null for none.</param>
public sealed record PersonCollectorFacultyArgs(string? DepartmentList, string? JobGroups, bool Emeriti);

/// <summary>Arguments for usp_get_personCollectorStaff.</summary>
public sealed record PersonCollectorStaffArgs(string? DepartmentList, bool IncludeMsp, bool IncludePss, string? VetJobGroups);

/// <summary>Arguments for usp_get_personCollectorStudents, apart from the current terms.</summary>
public sealed record PersonCollectorStudentArgs(string? ClassCodes, string? DegreeCodes);

/// <summary>
/// Which procedures to call for a request, worked out the way the legacy results page did. A
/// null member means that section wasn't asked for. Codes are joined with commas, as the
/// legacy form submitted them.
/// </summary>
public sealed record PersonCollectorPlan(
    string? FullDepartmentList,
    PersonCollectorFacultyArgs? Senate,
    PersonCollectorFacultyArgs? Federation,
    PersonCollectorStaffArgs? Staff,
    PersonCollectorStudentArgs? Students)
{
    public const string DepartmentKey = "department";
    public const string SenateKey = "senate";
    public const string FederationKey = "federation";
    public const string StaffKey = "staff";
    public const string StudentsKey = "students";

    /// <summary>The section titles, as the legacy page labeled them.</summary>
    public static readonly IReadOnlyDictionary<string, string> Titles = new Dictionary<string, string>
    {
        [DepartmentKey] = "Full Department List",
        [SenateKey] = "Senate Faculty",
        [FederationKey] = "Federation Faculty",
        [StaffKey] = "Staff Employees",
        [StudentsKey] = "Students",
    };

    /// <summary>True when the request asks for no section at all.</summary>
    public bool IsEmpty => FullDepartmentList is null && Senate is null && Federation is null && Staff is null && Students is null;

    /// <summary>
    /// The option keys in <paramref name="request"/> that aren't on the form. A request with any
    /// is refused rather than quietly narrowed.
    /// </summary>
    public static IReadOnlyList<string> UnknownKeys(PersonCollectorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return
        [
            .. Unknown(request.Departments, PersonCollectorOptions.Departments),
            .. Unknown(request.SenateGroups, PersonCollectorOptions.SenateGroups),
            .. Unknown(request.FederationGroups, PersonCollectorOptions.FederationGroups),
            .. Unknown(request.StudentClasses, PersonCollectorOptions.StudentClasses),
        ];
    }

    /// <summary>The plan for a request whose keys are all known (see <see cref="UnknownKeys"/>).</summary>
    public static PersonCollectorPlan Create(PersonCollectorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? departments = Codes(request.Departments, PersonCollectorOptions.Departments);
        string? senateGroups = Codes(request.SenateGroups, PersonCollectorOptions.SenateGroups);
        string? federationGroups = Codes(request.FederationGroups, PersonCollectorOptions.FederationGroups);
        string? classes = Codes(request.StudentClasses, PersonCollectorOptions.StudentClasses);
        string? vetTitles = request.StaffVeterinarians ? string.Join(',', PersonCollectorOptions.VeterinarianTitleCodes) : null;
        string? degrees = request.StudentMpvm ? PersonCollectorOptions.MpvmDegree : null;

        return new PersonCollectorPlan(
            request.FullDepartmentList ? departments : null,
            senateGroups is not null || request.SenateEmeriti
                ? new PersonCollectorFacultyArgs(departments, senateGroups, request.SenateEmeriti)
                : null,
            federationGroups is not null
                ? new PersonCollectorFacultyArgs(departments, federationGroups, Emeriti: false)
                : null,
            request.StaffMsp || request.StaffPss || vetTitles is not null
                ? new PersonCollectorStaffArgs(departments, request.StaffMsp, request.StaffPss, vetTitles)
                : null,
            classes is not null || degrees is not null
                ? new PersonCollectorStudentArgs(classes, degrees)
                : null);
    }

    private static IEnumerable<string> Unknown(IReadOnlyList<string>? keys, IReadOnlyList<PersonCollectorOption> options)
    {
        return (keys ?? []).Where(key => !options.Any(option => string.Equals(option.Key, key, StringComparison.Ordinal)));
    }

    /// <summary>The chosen options' codes in form order, comma-separated; null when none are chosen.</summary>
    private static string? Codes(IReadOnlyList<string>? keys, IReadOnlyList<PersonCollectorOption> options)
    {
        HashSet<string> chosen = new(keys ?? [], StringComparer.Ordinal);
        List<string> codes = [.. options.Where(option => chosen.Contains(option.Key)).SelectMany(option => option.Codes)];
        return codes.Count > 0 ? string.Join(',', codes) : null;
    }
}
