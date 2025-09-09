using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Extensions
{
    /// <summary>
    /// Extension methods for mapping Service entities to DTOs
    /// </summary>
    public static class ServiceMappingExtensions
    {
        /// <summary>
        /// Maps a Service entity to ServiceDto
        /// </summary>
        /// <param name="service">The service entity to map</param>
        /// <returns>ServiceDto with mapped properties</returns>
        public static ServiceDto ToDto(this Service service)
        {
            return new ServiceDto
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                ShortName = service.ShortName,
                WeekSize = service.WeekSize,
                ScheduleEditPermission = service.ScheduleEditPermission,
                UserCanEdit = null // This should be set by the caller based on user permissions
            };
        }

        /// <summary>
        /// Maps a Service entity to ServiceDto with user edit permission
        /// </summary>
        /// <param name="service">The service entity to map</param>
        /// <param name="userCanEdit">Whether the current user can edit this service</param>
        /// <returns>ServiceDto with mapped properties and user permissions</returns>
        public static ServiceDto ToDto(this Service service, bool userCanEdit)
        {
            return new ServiceDto
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                ShortName = service.ShortName,
                WeekSize = service.WeekSize,
                ScheduleEditPermission = service.ScheduleEditPermission,
                UserCanEdit = userCanEdit
            };
        }

        /// <summary>
        /// Maps a collection of Service entities to ServiceDto collection
        /// </summary>
        /// <param name="services">The service entities to map</param>
        /// <returns>Collection of ServiceDto</returns>
        public static IEnumerable<ServiceDto> ToDto(this IEnumerable<Service> services)
        {
            return services.Select(service => service.ToDto());
        }

        /// <summary>
        /// Maps a collection of Service entities to ServiceDto collection with user permissions
        /// </summary>
        /// <param name="services">The service entities to map</param>
        /// <param name="userCanEditLookup">Lookup function to determine if user can edit each service</param>
        /// <returns>Collection of ServiceDto with user permissions</returns>
        public static IEnumerable<ServiceDto> ToDto(this IEnumerable<Service> services,
            Func<int, bool> userCanEditLookup)
        {
            return services.Select(service => service.ToDto(userCanEditLookup(service.ServiceId)));
        }
    }
}
