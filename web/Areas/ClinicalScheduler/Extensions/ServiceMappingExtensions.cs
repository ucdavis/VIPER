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
                WeekSize = service.WeekSize,
                ScheduleEditPermission = service.ScheduleEditPermission
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

    }
}
