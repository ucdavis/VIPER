using Viper.Models.ClinicalScheduler;
using Viper.Areas.ClinicalScheduler.Controllers;

namespace Viper.Areas.ClinicalScheduler.Extensions
{
    /// <summary>
    /// Extension methods for mapping InstructorSchedule entities to DTOs
    /// </summary>
    public static class InstructorScheduleMappingExtensions
    {
        /// <summary>
        /// Maps an InstructorSchedule entity to InstructorScheduleResponse DTO
        /// </summary>
        /// <param name="schedule">The InstructorSchedule entity</param>
        /// <param name="canRemove">Whether the schedule can be removed</param>
        /// <returns>InstructorScheduleResponse DTO</returns>
        public static InstructorScheduleResponse ToResponse(this InstructorSchedule schedule, bool canRemove = false)
        {
            return new InstructorScheduleResponse
            {
                InstructorScheduleId = schedule.InstructorScheduleId,
                MothraId = schedule.MothraId,
                RotationId = schedule.RotationId,
                WeekId = schedule.WeekId,
                IsPrimaryEvaluator = schedule.Evaluator,
                CanRemove = canRemove
            };
        }

        /// <summary>
        /// Maps a collection of InstructorSchedule entities to conflict response format
        /// </summary>
        /// <param name="conflicts">Collection of conflicting schedules</param>
        /// <param name="message">Conflict message</param>
        /// <returns>ScheduleConflictResponse</returns>
        public static ScheduleConflictResponse ToConflictResponse(this IEnumerable<InstructorSchedule> conflicts, string message)
        {
            return new ScheduleConflictResponse
            {
                Conflicts = conflicts.Select(c => c.ToResponse()).ToList(),
                Message = message
            };
        }
    }
}
