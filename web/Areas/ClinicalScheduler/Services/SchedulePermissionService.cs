using Microsoft.EntityFrameworkCore;
using Viper.Classes.SQLContext;
using Viper.Models.AAUD;
using Viper.Models.ClinicalScheduler;

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
        /// Check if the current user has edit permissions for a specific service
        /// </summary>
        public async Task<bool> HasEditPermissionForServiceAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = GetValidatedCurrentUser($"checking edit permissions for service {serviceId}");
                if (user == null) return false;

                // If user has manage permission, they can edit everything
                if (_userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Manage))
                {
                    _logger.LogDebug("User {MothraId} has manage permission for service {ServiceId}", user.MothraId, serviceId);
                    return true;
                }

                var requiredPermission = await GetRequiredPermissionForServiceAsync(serviceId, cancellationToken);
                var hasPermission = _userHelper.HasPermission(_rapsContext, user, requiredPermission);

                _logger.LogDebug("User {MothraId} permission check for service {ServiceId}: required='{RequiredPermission}', hasPermission={HasPermission}",
                    user.MothraId, serviceId, requiredPermission, hasPermission);

                return hasPermission;
            }
            catch (Exception ex)
            {
                var currentUser = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error checking edit permissions for user {MothraId} and service {ServiceId}", currentUser?.MothraId ?? "unknown", serviceId);
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
                _logger.LogError(ex, "Error checking edit permissions for user {MothraId} and rotation {RotationId}", user?.MothraId ?? "unknown", rotationId);
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

                // If user has manage permission, they can edit all services
                if (_userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Manage))
                {
                    _logger.LogDebug("User {MothraId} has manage permission, returning all services", user.MothraId);
                    return allServices;
                }

                // Filter services based on specific permissions
                var editableServices = allServices
                    .Where(service => _userHelper.HasPermission(_rapsContext, user, GetEffectivePermission(service)))
                    .ToList();

                _logger.LogDebug("User {MothraId} can edit {EditableCount} out of {TotalCount} services",
                    user.MothraId, editableServices.Count, allServices.Count);

                return editableServices;
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error getting editable services for user {MothraId}", user?.MothraId ?? "unknown");
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
                var hasManagePermission = _userHelper.HasPermission(_rapsContext, user, ClinicalSchedulePermissions.Manage);

                var permissions = services.ToDictionary(
                    service => service.ServiceId,
                    service => hasManagePermission || _userHelper.HasPermission(_rapsContext, user, GetEffectivePermission(service))
                );

                var editableCount = permissions.Count(kvp => kvp.Value);
                _logger.LogDebug("User {MothraId} can edit {EditableCount} out of {TotalCount} services (hasManage: {HasManage})",
                    user.MothraId, editableCount, permissions.Count, hasManagePermission);

                return permissions;
            }
            catch (Exception ex)
            {
                var user = _userHelper.GetCurrentUser();
                _logger.LogError(ex, "Error getting service permissions for user {MothraId}", user?.MothraId ?? "unknown");
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
    }
}