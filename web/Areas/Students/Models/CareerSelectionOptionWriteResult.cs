namespace Viper.Areas.Students.Models;

public enum CareerSelectionOptionWriteStatus
{
    Success,
    NotFound,
    // The request itself is unusable: a blank or overlong name, or an attempt to change the
    // catch-all option.
    Invalid,
    // The request is fine but the current data refuses it: a duplicate name, or an option in use.
    Conflict,
}

/// <summary>
/// The outcome of adding, renaming or deleting a career selection dropdown option. Option is set
/// for a successful add or rename; Error is set for anything other than success or not found.
/// </summary>
public sealed record CareerSelectionOptionWriteResult(
    CareerSelectionOptionWriteStatus Status,
    CareerSelectionOptionDto? Option = null,
    string? Error = null)
{
    public static CareerSelectionOptionWriteResult Success(CareerSelectionOptionDto? option = null) =>
        new(CareerSelectionOptionWriteStatus.Success, option);

    public static CareerSelectionOptionWriteResult NotFound() =>
        new(CareerSelectionOptionWriteStatus.NotFound);

    public static CareerSelectionOptionWriteResult Invalid(string error) =>
        new(CareerSelectionOptionWriteStatus.Invalid, Error: error);

    public static CareerSelectionOptionWriteResult Conflict(string error) =>
        new(CareerSelectionOptionWriteStatus.Conflict, Error: error);
}
