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

        /// <summary>
        /// Check if the current user can edit their own schedule for a specific instructor schedule entry
        /// </summary>
        /// <param name="instructorScheduleId">Instructor schedule ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if current user can edit their own schedule for this entry</returns>
        Task<bool> CanEditOwnScheduleAsync(int instructorScheduleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check access to student schedules with the given parameters
        /// - Students can view their own schedule (if they have the MySchedule permission)
        /// - Students can also view the schedule for a rotation/week they are on (NOT IMPLEMENTED for CTS)
        /// - Accommodation users can view marked students (NOT IMPLEMENTED for CTS)
        /// - Otherwise, managers and ViewStdSchedules can view all schedules
        /// </summary>
        /// <param name="mothraId">Student's mothra ID to check access for</param>
        /// <param name="rotationId">Rotation ID filter</param>
        /// <param name="serviceId">Service ID filter</param>
        /// <param name="weekId">Week ID filter</param>
        /// <param name="startDate">Start date filter</param>
        /// <param name="endDate">End date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if current user has access to view student schedules with given parameters</returns>
        Task<bool> CheckStudentScheduleParamsAsync(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check access to instructor schedules with the given parameters
        /// - Manage access grants access to all schedules
        /// - Those with ViewClnSchedules can view all instructor schedules
        /// - Instructors can always view their own schedule
        /// </summary>
        /// <param name="mothraId">Instructor's mothra ID to check access for</param>
        /// <param name="rotationId">Rotation ID filter</param>
        /// <param name="serviceId">Service ID filter</param>
        /// <param name="weekId">Week ID filter</param>
        /// <param name="startDate">Start date filter</param>
        /// <param name="endDate">End date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if current user has access to view instructor schedules with given parameters</returns>
        Task<bool> CheckInstructorScheduleParamsAsync(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    }
}
