using Viper.Models.ClinicalScheduler;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service interface for handling schedule editing permissions based on service-specific permissions
    /// </summary>
    public interface ISchedulePermissionService
    {
        /// <summary>
        /// Check if the current user has edit permissions for a specific service
        /// </summary>
        /// <param name="serviceId">Service ID to check permissions for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if current user has edit permissions for the service</returns>
        Task<bool> HasEditPermissionForServiceAsync(int serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the current user has edit permissions for a rotation (via its associated service)
        /// </summary>
        /// <param name="rotationId">Rotation ID to check permissions for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if current user has edit permissions for the rotation's service</returns>
        Task<bool> HasEditPermissionForRotationAsync(int rotationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all services that the current user can edit
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of services the current user can edit</returns>
        Task<List<Service>> GetUserEditableServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current user's effective permissions for all services
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary mapping service ID to whether current user can edit it</returns>
        Task<Dictionary<int, bool>> GetUserServicePermissionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the required permission string for editing a specific service
        /// </summary>
        /// <param name="serviceId">Service ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Required permission string, or default manage permission if none specified</returns>
        Task<string> GetRequiredPermissionForServiceAsync(int serviceId, CancellationToken cancellationToken = default);
    }
}