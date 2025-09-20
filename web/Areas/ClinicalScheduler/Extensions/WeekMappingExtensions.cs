using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Extensions
{
    /// <summary>
    /// Extension methods for mapping Week entities to DTOs
    /// </summary>
    public static class WeekMappingExtensions
    {
        /// <summary>
        /// Maps a VWeek entity to a WeekDto
        /// </summary>
        /// <param name="week">The VWeek entity to map</param>
        /// <returns>A WeekDto representation of the entity</returns>
        public static WeekDto ToDto(this VWeek week)
        {
            return new WeekDto
            {
                WeekId = week.WeekId,
                WeekNum = week.WeekNum,
                DateStart = week.DateStart,
                DateEnd = week.DateEnd,
                ExtendedRotation = week.ExtendedRotation,
                TermCode = week.TermCode,
                StartWeek = week.StartWeek,
                ForcedVacation = week.ForcedVacation,
                GradYear = week.GradYear,
                WeekGradYearId = week.WeekGradYearId
            };
        }

        /// <summary>
        /// Maps a collection of VWeek entities to WeekDto collection
        /// </summary>
        /// <param name="weeks">The VWeek entities to map</param>
        /// <returns>A collection of WeekDto representations</returns>
        public static IEnumerable<WeekDto> ToDto(this IEnumerable<VWeek> weeks)
        {
            return weeks.Select(w => w.ToDto());
        }

        /// <summary>
        /// Maps a list of VWeek entities to a list of WeekDto
        /// </summary>
        /// <param name="weeks">The VWeek entities to map</param>
        /// <returns>A list of WeekDto representations</returns>
        public static List<WeekDto> ToDto(this List<VWeek> weeks)
        {
            return weeks.Select(w => w.ToDto()).ToList();
        }
    }
}
