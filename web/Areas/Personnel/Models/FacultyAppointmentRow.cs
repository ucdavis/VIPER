namespace Viper.Areas.Personnel.Models;

/// <summary>
/// One paid appointment of a faculty member, from <c>usp_get_facultyList</c> with every
/// employee's details. Age is computed by the procedure, as in the legacy report.
/// </summary>
public sealed record FacultyAppointmentRow(
    string EmployeeId,
    string Name,
    int? Age,
    DateOnly? HireDate,
    string? Department,
    string? Title);
