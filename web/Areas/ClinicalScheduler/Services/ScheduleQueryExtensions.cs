using Viper.Models.CTS;

namespace Viper.Areas.ClinicalScheduler.Services;

/// <summary>
/// LINQ extensions shared between InstructorScheduleService and StudentScheduleService
/// for filtering schedule entities by the common rotation / service / week / date filters.
/// </summary>
public static class ScheduleQueryExtensions
{
    public static IQueryable<T> ApplyScheduleFilters<T>(
        this IQueryable<T> query,
        int? classYear,
        string? mothraId,
        int? rotationId,
        int? serviceId,
        int? weekId,
        DateTime? startDate,
        DateTime? endDate)
        where T : class, IScheduleEntity
    {
        if (classYear.HasValue)
        {
            query = query.Where(s => s.Week.WeekGradYears.Any(gy => gy.GradYear == classYear));
        }

        if (!string.IsNullOrWhiteSpace(mothraId))
        {
            query = query.Where(s => s.MothraId == mothraId);
        }

        if (rotationId.HasValue)
        {
            query = query.Where(s => s.RotationId == rotationId);
        }

        if (serviceId.HasValue)
        {
            query = query.Where(s => s.ServiceId == serviceId);
        }

        if (weekId.HasValue)
        {
            query = query.Where(s => s.WeekId == weekId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(s => s.DateEnd >= startDate);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.DateStart <= endDate);
        }

        return query;
    }
}
