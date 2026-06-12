namespace Viper.Models.CTS;

/// <summary>
/// Common filterable fields exposed by clinical-schedule entities (instructor and student).
/// Used by ScheduleQueryExtensions to apply shared LINQ filter clauses generically.
/// </summary>
public interface IScheduleEntity
{
    string MothraId { get; }
    int RotationId { get; }
    int ServiceId { get; }
    int WeekId { get; }
    DateTime DateStart { get; }
    DateTime DateEnd { get; }
    Week Week { get; }
}
