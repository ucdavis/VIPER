namespace Viper.Areas.Personnel.Models;

/// <summary>
/// One appointment from <c>usp_get_flsa</c>. <see cref="EmployeeId"/> isn't shown; it counts
/// people once when they hold several appointments.
/// </summary>
public sealed record FlsaEmployeeRow(
    string? EmployeeId,
    string Name,
    string? Department,
    string? Title,
    string? Flsa,
    string? Email);
