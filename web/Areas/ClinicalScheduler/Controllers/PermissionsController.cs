using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    /// <summary>
    /// Controller for managing user permissions and access control in Clinical Scheduler
    /// </summary>
    [Route("api/clinicalscheduler/permissions")]
    [ApiController]
    [Area("ClinicalScheduler")]
    [Permission(Allow = ClinicalSchedulePermissions.Base)]
    public class PermissionsController : BaseClinicalSchedulerController
    {
        private readonly ISchedulePermissionService _permissionService;
        private readonly IUserHelper _userHelper;
        private readonly ClinicalSchedulerContext _csContext;

        public PermissionsController(
            ISchedulePermissionService permissionService,
            IGradYearService gradYearService,
            ClinicalSchedulerContext csContext,
            ILogger<PermissionsController> logger,
            IUserHelper? userHelper = null)
            : base(gradYearService, logger)
        {
            _permissionService = permissionService;
            _userHelper = userHelper ?? new UserHelper();
            _csContext = csContext;
        }

        /// <summary>
        /// Get the current user's service-level edit permissions
        /// </summary>
        /// <returns>Dictionary mapping service IDs to edit permissions</returns>
        [HttpGet("user")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetUserPermissions()
        {
            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when getting user permissions");
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Getting permissions for user {MothraId}", user.MothraId);

                // Get service permissions and editable services
                var servicePermissions = await _permissionService.GetUserServicePermissionsAsync(HttpContext.RequestAborted);
                var editableServices = await _permissionService.GetUserEditableServicesAsync(HttpContext.RequestAborted);

                // Check for general manage permission
                var hasManagePermission = _userHelper.HasPermission(
                    HttpContext.RequestServices.GetRequiredService<RAPSContext>(),
                    user,
                    ClinicalSchedulePermissions.Manage
                );

                var response = new
                {
                    user = new
                    {
                        mothraId = user.MothraId,
                        displayName = user.DisplayFullName
                    },
                    permissions = new
                    {
                        hasManagePermission = hasManagePermission,
                        servicePermissions = servicePermissions,
                        editableServiceCount = editableServices.Count
                    },
                    editableServices = editableServices.Select(s => new
                    {
                        serviceId = s.ServiceId,
                        serviceName = s.ServiceName,
                        shortName = s.ShortName,
                        scheduleEditPermission = s.ScheduleEditPermission
                    }).ToList()
                };

                _logger.LogInformation("Retrieved permissions for user {MothraId}: hasManage={HasManage}, editableServices={EditableCount}",
                    user.MothraId, hasManagePermission, editableServices.Count);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve user permissions");
            }
        }

        /// <summary>
        /// Check if current user can edit a specific service
        /// </summary>
        /// <param name="serviceId">Service ID to check</param>
        /// <returns>Permission check result</returns>
        [HttpGet("service/{serviceId:int}/can-edit")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CanEditService(int serviceId)
        {
            if (serviceId <= 0)
            {
                _logger.LogWarning("Invalid service ID provided for permission check: {ServiceId}", serviceId);
                return BadRequest(new { error = "Service ID must be a positive integer", serviceId });
            }

            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when checking service edit permission for service {ServiceId}", serviceId);
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Checking edit permission for user {MothraId} and service {ServiceId}", user.MothraId, serviceId);

                var canEdit = await _permissionService.HasEditPermissionForServiceAsync(serviceId, HttpContext.RequestAborted);
                var requiredPermission = await _permissionService.GetRequiredPermissionForServiceAsync(serviceId, HttpContext.RequestAborted);

                var response = new
                {
                    serviceId = serviceId,
                    canEdit = canEdit,
                    requiredPermission = requiredPermission,
                    user = new
                    {
                        mothraId = user.MothraId
                    }
                };

                _logger.LogInformation("Permission check result for user {MothraId} and service {ServiceId}: canEdit={CanEdit}, required='{RequiredPermission}'",
                    user.MothraId, serviceId, canEdit, requiredPermission);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to check service edit permissions", "ServiceId", serviceId);
            }
        }

        /// <summary>
        /// Check if current user can edit a specific rotation
        /// </summary>
        /// <param name="rotationId">Rotation ID to check</param>
        /// <returns>Permission check result</returns>
        [HttpGet("rotation/{rotationId:int}/can-edit")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CanEditRotation(int rotationId)
        {
            if (rotationId <= 0)
            {
                _logger.LogWarning("Invalid rotation ID provided for permission check: {RotationId}", rotationId);
                return BadRequest(new { error = "Rotation ID must be a positive integer", rotationId });
            }

            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when checking rotation edit permission for rotation {RotationId}", rotationId);
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Checking edit permission for user {MothraId} and rotation {RotationId}", user.MothraId, rotationId);

                var canEdit = await _permissionService.HasEditPermissionForRotationAsync(rotationId, HttpContext.RequestAborted);

                var response = new
                {
                    rotationId = rotationId,
                    canEdit = canEdit,
                    user = new
                    {
                        mothraId = user.MothraId
                    }
                };

                _logger.LogInformation("Permission check result for user {MothraId} and rotation {RotationId}: canEdit={CanEdit}",
                    user.MothraId, rotationId, canEdit);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to check rotation edit permissions", "RotationId", rotationId);
            }
        }

        /// <summary>
        /// Get summary of permission system configuration
        /// </summary>
        /// <returns>Permission system summary</returns>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> GetPermissionSummary()
        {
            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when getting permission summary");
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Getting permission summary for user {MothraId}", user.MothraId);

                // Enforce elevated permission for full system-wide summary
                var raps = HttpContext.RequestServices.GetRequiredService<RAPSContext>();
                if (!_userHelper.HasPermission(raps, user, ClinicalSchedulePermissions.Manage))
                {
                    _logger.LogWarning("User {MothraId} attempted to access full permission summary without manage permission", user.MothraId);
                    return Forbid();
                }

                // Get all services and their permissions
                var services = await _csContext.Services
                    .AsNoTracking()
                    .OrderBy(s => s.ServiceName)
                    .Select(s => new
                    {
                        s.ServiceId,
                        s.ServiceName,
                        s.ShortName,
                        s.ScheduleEditPermission
                    })
                    .ToListAsync(HttpContext.RequestAborted);

                var userPermissions = await _permissionService.GetUserServicePermissionsAsync(HttpContext.RequestAborted);

                var servicesWithPermissions = services.Select(s => new
                {
                    s.ServiceId,
                    s.ServiceName,
                    s.ShortName,
                    hasCustomPermission = !string.IsNullOrEmpty(s.ScheduleEditPermission),
                    userCanEdit = userPermissions.GetValueOrDefault(s.ServiceId, false)
                }).ToList();

                var totalEditableServices = servicesWithPermissions.Count(s => s.userCanEdit);
                var servicesWithCustomPermissions = servicesWithPermissions.Count(s => s.hasCustomPermission);

                var response = new
                {
                    user = new
                    {
                        mothraId = user.MothraId,
                        displayName = user.DisplayFullName
                    },
                    summary = new
                    {
                        totalServices = services.Count,
                        editableServices = totalEditableServices,
                        servicesWithCustomPermissions = servicesWithCustomPermissions,
                        defaultPermission = ClinicalSchedulePermissions.Manage
                    },
                    services = servicesWithPermissions
                };

                _logger.LogInformation("Permission summary for user {MothraId}: total={Total}, editable={Editable}, custom={Custom}",
                    user.MothraId, services.Count, totalEditableServices, servicesWithCustomPermissions);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Failed to retrieve permission summary");
            }
        }
    }
}