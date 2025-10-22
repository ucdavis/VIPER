using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.ClinicalScheduler;
using VIPER.Areas.ClinicalScheduler.Utilities;

namespace Viper.Areas.ClinicalScheduler.Services
{
    /// <summary>
    /// Service for handling schedule editing permissions based on service-specific permissions
    /// </summary>
    public class SchedulePermissionService : ISchedulePermissionService
    {
        private readonly ClinicalSchedulerContext _context;
        private readonly RAPSContext _rapsContext;
        private readonly ILogger<SchedulePermissionService> _logger;
        private readonly IUserHelper _userHelper;

        public SchedulePermissionService(
            ClinicalSchedulerContext context,
            RAPSContext rapsContext,
            ILogger<SchedulePermissionService> logger,
            IUserHelper? userHelper = null)
        {
            _context = context;
            _rapsContext = rapsContext;
            _logger = logger;
            _userHelper = userHelper ?? new UserHelper();
        }

        /// <summary>
        /// Get the effective permission required for a service (custom or default)
        /// </summary>
        private static string GetEffectivePermission(Service service)
        {
            return string.IsNullOrEmpty(service.ScheduleEditPermission)
                ? ClinicalSchedulePermissions.Manage
                : service.ScheduleEditPermission;
        }

        /// <summary>
        /// Get current user and validate authentication
        /// </summary>
        private AaudUser? GetValidatedCurrentUser(string contextInfo)
        {
            var user = _userHelper.GetCurrentUser();
            if (user == null)
            {
                _logger.LogWarning("No current user found when {ContextInfo}", contextInfo);
            }
            return user;
        }

        /// <summary>
        /// Check if user has full access permissions (Admin > Manage > EditClnSchedules)
        /// </summary>
        private bool HasFullAccessPermission(AaudUser user)
        {
            return _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Admin) ||
                   _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Manage) ||
                   _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.EditClnSchedules);
        }

        /// <summary>
        /// Check if the current user has edit permissions for a specific service
        /// Enhanced with permission hierarchy (Admin > Manage > EditClnSchedules > Service-specific)
        /// </summary>
        public async Task<bool> HasEditPermissionForServiceAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = GetValidatedCurrentUser($"checking edit permissions for service {serviceId}");
                if (user == null) return false;

                if (HasFullAccessPermission(user))
                {
                    _logger.LogDebug("User {MothraId} has full access permission for service {ServiceId}", LogSanitizer.SanitizeId(user.MothraId), serviceId);
                    return true;
                }

                // Check service-specific permission
                var requiredPermission = await GetRequiredPermissionForServiceAsync(serviceId, cancellationToken);
                var hasPermission = _userHelper.HasPermission(_rapsContext, user, requiredPermission);

                _logger.LogDebug("User {MothraId} permission check for service {ServiceId}: required='{RequiredPermission}', hasPermission={HasPermission}",
                    LogSanitizer.SanitizeId(user.MothraId), serviceId, requiredPermission, hasPermission);

                return hasPermission;
            }
            catch (Exception ex)
            {
                var currentUser = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error checking edit permissions for user {MothraId} and service {ServiceId}", LogSanitizer.SanitizeId(currentUser?.MothraId), serviceId);
                return false;
            }
        }

        /// <summary>
        /// Check if the current user has edit permissions for a rotation (via its associated service)
        /// </summary>
        public async Task<bool> HasEditPermissionForRotationAsync(int rotationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var rotation = await _context.Rotations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RotId == rotationId, cancellationToken);

                if (rotation == null)
                {
                    _logger.LogWarning("Rotation {RotationId} not found when checking edit permissions", rotationId);
                    return false;
                }

                return await HasEditPermissionForServiceAsync(rotation.ServiceId, cancellationToken);
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error checking edit permissions for user {MothraId} and rotation {RotationId}", LogSanitizer.SanitizeId(user?.MothraId), rotationId);
                return false;
            }
        }

        /// <summary>
        /// Get all services that the current user can edit
        /// </summary>
        public async Task<List<Service>> GetUserEditableServicesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var user = GetValidatedCurrentUser("getting editable services for current user");
                if (user == null) return new List<Service>();

                var allServices = await _context.Services.AsNoTracking().ToListAsync(cancellationToken);

                if (HasFullAccessPermission(user))
                {
                    _logger.LogDebug("User {MothraId} has full access permission, returning all services", LogSanitizer.SanitizeId(user.MothraId));
                    return allServices;
                }

                // Filter services based on specific permissions
                var editableServices = allServices
                    .Where(service => _userHelper.HasPermission(_rapsContext, user, GetEffectivePermission(service)))
                    .ToList();

                _logger.LogDebug("User {MothraId} can edit {EditableCount} out of {TotalCount} services",
                    LogSanitizer.SanitizeId(user.MothraId), editableServices.Count, allServices.Count);

                return editableServices;
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error getting editable services for user {MothraId}", LogSanitizer.SanitizeId(user?.MothraId));
                return new List<Service>();
            }
        }

        /// <summary>
        /// Get current user's effective permissions for all services
        /// </summary>
        public async Task<Dictionary<int, bool>> GetUserServicePermissionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var user = GetValidatedCurrentUser("getting service permissions for current user");
                if (user == null) return new Dictionary<int, bool>();

                var services = await _context.Services.AsNoTracking().ToListAsync(cancellationToken);

                var hasFullAccess = HasFullAccessPermission(user);

                var permissions = services.ToDictionary(
                    service => service.ServiceId,
                    service => hasFullAccess || _userHelper.HasPermission(_rapsContext, user, GetEffectivePermission(service))
                );

                var editableCount = permissions.Count(kvp => kvp.Value);
                _logger.LogDebug("User {MothraId} can edit {EditableCount} out of {TotalCount} services (hasFullAccess: {HasFullAccess})",
                    LogSanitizer.SanitizeId(user.MothraId), editableCount, permissions.Count, hasFullAccess);

                return permissions;
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error getting service permissions for user {MothraId}", LogSanitizer.SanitizeId(user?.MothraId));
                return new Dictionary<int, bool>();
            }
        }

        /// <summary>
        /// Get the required permission string for editing a specific service
        /// </summary>
        public async Task<string> GetRequiredPermissionForServiceAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var service = await _context.Services
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ServiceId == serviceId, cancellationToken);

                if (service == null)
                {
                    _logger.LogWarning("Service {ServiceId} not found when getting required permission", serviceId);
                    return ClinicalSchedulePermissions.Manage;
                }

                var requiredPermission = GetEffectivePermission(service);
                _logger.LogDebug("Required permission for service {ServiceId}: {RequiredPermission}", serviceId, requiredPermission);
                return requiredPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting required permission for service {ServiceId}", serviceId);
                return ClinicalSchedulePermissions.Manage;
            }
        }

        /// <summary>
        /// Check if the current user can edit their own schedule for a specific instructor schedule entry
        /// </summary>
        /// <param name="instructorScheduleId">Instructor schedule ID to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if current user can edit their own schedule for this entry</returns>
        public async Task<bool> CanEditOwnScheduleAsync(int instructorScheduleId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = GetValidatedCurrentUser($"checking own schedule edit permissions for instructor schedule {instructorScheduleId}");
                if (user == null) return false;

                // First check if user has EditOwnSchedule permission
                if (!_userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.EditOwnSchedule))
                {
                    _logger.LogDebug("User {MothraId} does not have EditOwnSchedule permission", LogSanitizer.SanitizeId(user.MothraId));
                    return false;
                }

                // Get the instructor schedule entry
                var instructorSchedule = await _context.InstructorSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, cancellationToken);

                if (instructorSchedule == null)
                {
                    _logger.LogDebug("Instructor schedule not found when checking own schedule permissions");
                    return false;
                }

                // Check if the schedule belongs to the current user
                var canEdit = instructorSchedule.MothraId == user.MothraId;

                _logger.LogDebug("User {MothraId} own schedule edit check for instructor schedule {InstructorScheduleId}: scheduleOwner={ScheduleOwner}, canEdit={CanEdit}",
                    LogSanitizer.SanitizeId(user.MothraId), instructorScheduleId, LogSanitizer.SanitizeId(instructorSchedule.MothraId), canEdit);

                return canEdit;
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error checking own schedule permissions for user {MothraId} and instructor schedule {InstructorScheduleId}",
                    LogSanitizer.SanitizeId(user?.MothraId), instructorScheduleId);
                return false;
            }
        }

        /// <summary>
        /// Check access to student schedules with the given parameters
        /// </summary>
        /// <remarks>
        /// This method uses Task.FromResult as a synchronous fast-path optimization since it only performs
        /// in-memory permission checks without any I/O operations. This avoids unnecessary async overhead.
        /// </remarks>
        public Task<bool> CheckStudentScheduleParamsAsync(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = GetValidatedCurrentUser("checking student schedule access");
                if (user == null) return Task.FromResult(false);

                // Check if user has manage or view students permission
                if (_userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Manage) ||
                    _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.ViewStudents))
                {
                    _logger.LogDebug("User {MothraId} has management or ViewStudents permission for student schedules", LogSanitizer.SanitizeId(user.MothraId));
                    return Task.FromResult(true);
                }

                // Check if user is viewing their own schedule
                if (mothraId != null && mothraId == user.MothraId &&
                    _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.ViewOwn))
                {
                    _logger.LogDebug("User {MothraId} is viewing their own schedule", LogSanitizer.SanitizeId(user.MothraId));
                    return Task.FromResult(true);
                }

                // Additional access rules not implemented (as noted in original comments)
                // - Students viewing schedules for rotations/weeks they are on
                // - Accommodation users viewing marked students

                _logger.LogDebug("User {MothraId} denied access to student schedules", LogSanitizer.SanitizeId(user.MothraId));
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error checking student schedule access for user {MothraId}", LogSanitizer.SanitizeId(user?.MothraId));
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Check access to instructor schedules with the given parameters
        /// </summary>
        /// <remarks>
        /// This method uses Task.FromResult as a synchronous fast-path optimization since it only performs
        /// in-memory permission checks without any I/O operations. This avoids unnecessary async overhead.
        /// </remarks>
        public Task<bool> CheckInstructorScheduleParamsAsync(string? mothraId, int? rotationId, int? serviceId, int? weekId, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = GetValidatedCurrentUser("checking instructor schedule access");
                if (user == null) return Task.FromResult(false);

                // Check if user has manage or view clinicians permission
                if (_userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Manage) ||
                    _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.ViewClinicians))
                {
                    _logger.LogDebug("User {MothraId} has management or ViewClinicians permission for instructor schedules", LogSanitizer.SanitizeId(user.MothraId));
                    return Task.FromResult(true);
                }

                // Check if user is viewing their own schedule (either with ViewOwn or EditOwnSchedule permission)
                if (mothraId != null && mothraId == user.MothraId &&
                    (_userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.ViewOwn) ||
                     _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.EditOwnSchedule)))
                {
                    _logger.LogDebug("User {MothraId} is viewing their own instructor schedule", LogSanitizer.SanitizeId(user.MothraId));
                    return Task.FromResult(true);
                }

                _logger.LogDebug("User {MothraId} denied access to instructor schedules", LogSanitizer.SanitizeId(user.MothraId));
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error checking instructor schedule access for user {MothraId}", LogSanitizer.SanitizeId(user?.MothraId));
                return Task.FromResult(false);
            }
        }
    }
}
