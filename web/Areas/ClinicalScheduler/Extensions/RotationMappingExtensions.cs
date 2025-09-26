using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Extensions
{
    /// <summary>
    /// Extension methods for mapping Rotation entities to DTOs
    /// </summary>
    public static class RotationMappingExtensions
    {
        /// <summary>
        /// Maps a Rotation entity to RotationDto
        /// </summary>
        /// <param name="rotation">The rotation entity to map</param>
        /// <param name="includeService">Whether to include service details</param>
        /// <returns>RotationDto with mapped properties</returns>
        public static RotationDto ToDto(this Rotation rotation, bool includeService = true)
        {
            return new RotationDto
            {
                RotId = rotation.RotId,
                Name = rotation.Name,
                Abbreviation = rotation.Abbreviation,
                SubjectCode = rotation.SubjectCode,
                CourseNumber = rotation.CourseNumber,
                ServiceId = rotation.ServiceId,
                Service = includeService && rotation.Service != null ? rotation.Service.ToDto() : null
            };
        }

        /// <summary>
        /// Maps a collection of Rotation entities to RotationDto collection
        /// </summary>
        /// <param name="rotations">The rotation entities to map</param>
        /// <param name="includeService">Whether to include service details</param>
        /// <returns>Collection of RotationDto</returns>
        public static IEnumerable<RotationDto> ToDto(this IEnumerable<Rotation> rotations,
            bool includeService = true)
        {
            return rotations.Select(rotation => rotation.ToDto(includeService));
        }
    }
}
