namespace Viper.Areas.Personnel.Models;

/// <summary>
/// One leave of absence from <c>usp_get_peopleOnLeave</c>, carrying only the columns the report shows.
/// </summary>
public sealed record EmployeeOnLeaveRow(
    string Name,
    string? HomeDepartment,
    DateOnly? LeaveStart,
    DateOnly? ReturnDate,
    string? Description,
    string? PayStatus);
