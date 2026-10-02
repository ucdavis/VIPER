using Viper.Areas.Reports.Engine;

namespace Viper.Areas.Reports.Services;

public enum ReportOutcomeStatus
{
    Ok,
    NotFound,
    Forbidden,
    Invalid,
}

/// <summary>
/// The result of a report request: a value, or the reason there isn't one. Keeps HTTP concerns
/// in the controller while the service stays testable without a request pipeline.
/// </summary>
public sealed record ReportOutcome<T>(ReportOutcomeStatus Status, T? Value, IReadOnlyList<ReportValidationError> Errors);

public static class ReportOutcome
{
    public static ReportOutcome<T> Ok<T>(T value)
    {
        return new ReportOutcome<T>(ReportOutcomeStatus.Ok, value, []);
    }

    public static ReportOutcome<T> NotFound<T>()
    {
        return new ReportOutcome<T>(ReportOutcomeStatus.NotFound, default, []);
    }

    public static ReportOutcome<T> Forbidden<T>()
    {
        return new ReportOutcome<T>(ReportOutcomeStatus.Forbidden, default, []);
    }

    /// <summary>
    /// Carries a failure status and its errors over to an outcome of another value type.
    /// </summary>
    internal static ReportOutcome<T> Failed<T>(ReportOutcomeStatus status, IReadOnlyList<ReportValidationError> errors)
    {
        return new ReportOutcome<T>(status, default, errors);
    }

    public static ReportOutcome<T> Invalid<T>(IReadOnlyList<ReportValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ReportOutcome<T>(ReportOutcomeStatus.Invalid, default, errors);
    }
}
