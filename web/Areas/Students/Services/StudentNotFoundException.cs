namespace Viper.Areas.Students.Services;

/// <summary>
/// Thrown when a person is not a current DVM student, or is one with no PIDM to key a record on.
/// Controllers translate this to 404 Not Found. A type of its own so that EF Core's internal
/// InvalidOperationExceptions still reach the 500 path rather than reading as a missing student.
/// </summary>
public sealed class StudentNotFoundException : InvalidOperationException
{
    public StudentNotFoundException(string message) : base(message) { }
}
