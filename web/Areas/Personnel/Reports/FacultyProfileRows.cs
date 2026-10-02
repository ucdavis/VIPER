using Viper.Areas.Personnel.Models;

namespace Viper.Areas.Personnel.Reports;

/// <summary>
/// One line of the Faculty Profile: an appointment, listed under a department where the person
/// holds an appointment. <see cref="Department"/> is the grouping; <see cref="AppointmentDepartment"/>
/// is this appointment's own department.
/// </summary>
public sealed record FacultyProfileRow(
    string Department,
    string EmployeeId,
    string Name,
    int? Age,
    DateOnly? HireDate,
    string? AppointmentDepartment,
    string? Title)
{
    public string? AgeGroup => FacultyProfileRows.AgeGroup(Age);

    public bool IsEmeritus => FacultyProfileRows.IsEmeritusTitle(Title);

    public bool IsRecalled => FacultyProfileRows.IsRecalledTitle(Title);

    public bool IsRetired => IsEmeritus || IsRecalled;
}

/// <summary>
/// The Faculty Profile's rules, kept apart from the report so each can be tested directly.
/// </summary>
public static class FacultyProfileRows
{
    /// <summary>
    /// Ages at or above this are highlighted, as in the legacy report.
    /// </summary>
    public const int HighlightAge = 60;

    private const int GroupWidth = 5;
    private const int YoungestGroup = 20;
    private const int OldestAge = 99;

    /// <summary>
    /// Five-year brackets from "20-24" to "95-99", matching <c>usp_get_profile_facultyAge</c>.
    /// Ages outside that range have no bracket.
    /// </summary>
    public static string? AgeGroup(int? age)
    {
        if (age is not { } years || years < YoungestGroup || years > OldestAge)
        {
            return null;
        }

        int start = years - (years % GroupWidth);
        return $"{start}-{start + GroupWidth - 1}";
    }

    public static bool IsEmeritusTitle(string? title)
    {
        return title?.Contains("EMERI", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static bool IsRecalledTitle(string? title)
    {
        return title?.Contains("RECAL", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Lists each person under every department where they hold an appointment, with all of their
    /// appointments each time, oldest people first, as the legacy report did with one query per
    /// department and one per person. This does it from a single query.
    /// </summary>
    public static IReadOnlyList<FacultyProfileRow> Expand(IReadOnlyList<FacultyAppointmentRow> appointments)
    {
        ArgumentNullException.ThrowIfNull(appointments);
        ILookup<string, FacultyAppointmentRow> byPerson =
            appointments.ToLookup(appointment => appointment.EmployeeId, StringComparer.Ordinal);

        return [.. appointments
            .Select(appointment => (Department: (appointment.Department ?? string.Empty).Trim(), appointment.EmployeeId))
            .Distinct()
            .SelectMany(member => byPerson[member.EmployeeId].Select(appointment => new FacultyProfileRow(
                member.Department,
                appointment.EmployeeId,
                appointment.Name,
                appointment.Age,
                appointment.HireDate,
                appointment.Department,
                appointment.Title)))
            .OrderByDescending(row => row.Age)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.EmployeeId, StringComparer.Ordinal)
            .ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)];
    }
}
