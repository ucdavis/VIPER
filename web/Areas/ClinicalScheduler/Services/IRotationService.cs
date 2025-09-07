using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Interface for rotation-related services in the Clinical Scheduler.
    /// Provides methods for retrieving rotation and service information.
    /// </summary>
    public interface IRotationService
    {
        /// <summary>
        /// Get all rotations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of rotations with their associated service data</returns>
        Task<List<Rotation>> GetRotationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific rotation by its ID
        /// </summary>
        /// <param name="rotationId">The rotation ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rotation with service data or null if not found</returns>
        Task<Rotation?> GetRotationAsync(int rotationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all rotations for a specific service
        /// </summary>
        /// <param name="serviceId">The service ID to filter by</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of rotations for the specified service</returns>
        Task<List<Rotation>> GetRotationsByServiceAsync(int serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all services
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all services</returns>
        Task<List<Service>> GetServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific service by its ID
        /// </summary>
        /// <param name="serviceId">The service ID to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service or null if not found</returns>
        Task<Service?> GetServiceAsync(int serviceId, CancellationToken cancellationToken = default);
    }
}
