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
        /// Maps a Rotation entity to RotationDto with user edit permissions
        /// </summary>
        /// <param name="rotation">The rotation entity to map</param>
        /// <param name="includeService">Whether to include service details</param>
        /// <param name="userCanEdit">Whether the current user can edit this rotation's service</param>
        /// <returns>RotationDto with mapped properties and user permissions</returns>
        public static RotationDto ToDto(this Rotation rotation, bool includeService, bool userCanEdit)
        {
            return new RotationDto
            {
                RotId = rotation.RotId,
                Name = rotation.Name,
                Abbreviation = rotation.Abbreviation,
                SubjectCode = rotation.SubjectCode,
                CourseNumber = rotation.CourseNumber,
                ServiceId = rotation.ServiceId,
                Service = includeService && rotation.Service != null
                    ? rotation.Service.ToDto(userCanEdit)
                    : null
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

        /// <summary>
        /// Maps a collection of Rotation entities to RotationDto collection with user permissions
        /// </summary>
        /// <param name="rotations">The rotation entities to map</param>
        /// <param name="includeService">Whether to include service details</param>
        /// <param name="userCanEditLookup">Lookup function to determine if user can edit each rotation's service</param>
        /// <returns>Collection of RotationDto with user permissions</returns>
        public static IEnumerable<RotationDto> ToDto(this IEnumerable<Rotation> rotations,
            bool includeService, Func<int, bool> userCanEditLookup)
        {
            return rotations.Select(rotation =>
                rotation.ToDto(includeService, userCanEditLookup(rotation.ServiceId)));
        }
    }
}
