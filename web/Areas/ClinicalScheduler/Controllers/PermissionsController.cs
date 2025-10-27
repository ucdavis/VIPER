using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.SQLContext;
using Web.Authorization;
using Viper.Classes.Utilities;

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
            var user = _userHelper.GetCurrentUser();

            if (user == null)
            {
                _logger.LogWarning("No current user found when getting user permissions");
                return Unauthorized(new { error = "User not authenticated" });
            }

            _logger.LogInformation("Getting permissions for user {MothraId}", LogSanitizer.SanitizeId(user.MothraId));

            // Get service permissions and editable services
            var servicePermissions = await _permissionService.GetUserServicePermissionsAsync(HttpContext.RequestAborted);
            var editableServices = await _permissionService.GetUserEditableServicesAsync(HttpContext.RequestAborted);

            // Check for all permission levels
            var rapsContext = HttpContext.RequestServices.GetRequiredService<RAPSContext>();
            var hasAdminPermission = _userHelper.HasPermission(rapsContext, user, ClinicalSchedulePermissions.Admin);
            var hasManagePermission = _userHelper.HasPermission(rapsContext, user, ClinicalSchedulePermissions.Manage);
            var hasEditClnSchedulesPermission = _userHelper.HasPermission(rapsContext, user, ClinicalSchedulePermissions.EditClnSchedules);
            var hasEditOwnSchedulePermission = _userHelper.HasPermission(rapsContext, user, ClinicalSchedulePermissions.EditOwnSchedule);

            var response = new
            {
                user = new
                {
                    mothraId = user.MothraId,
                    displayName = user.DisplayFullName
                },
                permissions = new
                {
                    hasAdminPermission,
                    hasManagePermission,
                    hasEditClnSchedulesPermission,
                    hasEditOwnSchedulePermission,
                    servicePermissions,
                    editableServiceCount = editableServices.Count
                },
                editableServices = editableServices.Select(s =>
                {
                    var serviceId = s.ServiceId;
                    var serviceName = s.ServiceName;
                    var scheduleEditPermission = s.ScheduleEditPermission;
                    return new
                    {
                        serviceId,
                        serviceName,
                        scheduleEditPermission
                    };
                }).ToList()
            };

            _logger.LogInformation("Retrieved permissions for user {MothraId}: hasManage={HasManage}, editableServices={EditableCount}",
                LogSanitizer.SanitizeId(user.MothraId), hasManagePermission, editableServices.Count);

            return Ok(response);
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

                // Check permission first to avoid information disclosure
                // The permission check will internally verify the service exists
                _logger.LogInformation("Checking edit permission for user {MothraId} and service {ServiceId}", LogSanitizer.SanitizeId(user.MothraId), serviceId);

                var canEdit = await _permissionService.HasEditPermissionForServiceAsync(serviceId, HttpContext.RequestAborted);
                var requiredPermission = await _permissionService.GetRequiredPermissionForServiceAsync(serviceId, HttpContext.RequestAborted);

                // If requiredPermission is null or empty, the service doesn't exist
                // Return a generic error without revealing existence information
                if (string.IsNullOrEmpty(requiredPermission))
                {
                    _logger.LogWarning("Service {ServiceId} not found or user {MothraId} lacks permission", serviceId, LogSanitizer.SanitizeId(user.MothraId));
                    return NotFound(new { error = "Service not found or access denied" });
                }

                var response = new
                {
                    serviceId,
                    canEdit,
                    requiredPermission,
                    user = new
                    {
                        mothraId = user.MothraId
                    }
                };

                _logger.LogInformation("Permission check result for user {MothraId} and service {ServiceId}: canEdit={CanEdit}, required='{RequiredPermission}'",
                    LogSanitizer.SanitizeId(user.MothraId), serviceId, canEdit, requiredPermission);

                return Ok(response);
            }
            catch (Exception)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("ServiceId", serviceId);
                throw; // Let ApiExceptionFilter handle the response
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

                // Check permission first to avoid information disclosure
                // The permission check will internally verify the rotation exists
                _logger.LogInformation("Checking edit permission for user {MothraId} and rotation {RotationId}", LogSanitizer.SanitizeId(user.MothraId), rotationId);

                var canEdit = await _permissionService.HasEditPermissionForRotationAsync(rotationId, HttpContext.RequestAborted);

                // Verify rotation exists - if not, return generic error to avoid information disclosure
                var rotationExists = await _csContext.Rotations
                    .AsNoTracking()
                    .AnyAsync(r => r.RotId == rotationId, HttpContext.RequestAborted);

                if (!rotationExists)
                {
                    _logger.LogWarning("Rotation {RotationId} not found or user {MothraId} lacks permission", rotationId, LogSanitizer.SanitizeId(user.MothraId));
                    return NotFound(new { error = "Rotation not found or access denied" });
                }

                var response = new
                {
                    rotationId,
                    canEdit,
                    user = new
                    {
                        mothraId = user.MothraId
                    }
                };

                _logger.LogInformation("Permission check result for user {MothraId} and rotation {RotationId}: canEdit={CanEdit}",
                    LogSanitizer.SanitizeId(user.MothraId), rotationId, canEdit);

                return Ok(response);
            }
            catch (Exception)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("RotationId", rotationId);
                throw; // Let ApiExceptionFilter handle the response
            }
        }

        /// <summary>
        /// Check if current user can edit their own schedule for a specific instructor schedule entry
        /// </summary>
        /// <param name="instructorScheduleId">Instructor schedule ID to check</param>
        /// <returns>Permission check result</returns>
        [HttpGet("instructor-schedule/{instructorScheduleId:int}/can-edit-own")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CanEditOwnSchedule(int instructorScheduleId)
        {
            if (instructorScheduleId <= 0)
            {
                _logger.LogWarning("Invalid instructor schedule ID provided for own schedule permission check: {InstructorScheduleId}", instructorScheduleId);
                return BadRequest(new { error = "Instructor schedule ID must be a positive integer", instructorScheduleId });
            }

            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when checking own schedule edit permission for instructor schedule {InstructorScheduleId}", instructorScheduleId);
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Atomically check for schedule existence and permission to avoid timing-based information disclosure
                _logger.LogInformation("Checking own schedule edit permission for user {MothraId} and instructor schedule {InstructorScheduleId}", LogSanitizer.SanitizeId(user.MothraId), instructorScheduleId);

                var schedule = await _csContext.InstructorSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, HttpContext.RequestAborted);

                if (schedule == null)
                {
                    _logger.LogWarning("Instructor schedule {InstructorScheduleId} not found or user {MothraId} lacks permission", instructorScheduleId, LogSanitizer.SanitizeId(user.MothraId));
                    return NotFound(new { error = "Instructor schedule not found or access denied" });
                }

                var canEdit = await _permissionService.CanEditOwnScheduleAsync(instructorScheduleId, HttpContext.RequestAborted);

                var response = new
                {
                    instructorScheduleId,
                    canEditOwn = canEdit,
                    user = new
                    {
                        mothraId = user.MothraId
                    }
                };

                _logger.LogDebug("Own schedule permission check result for user {MothraId} and instructor schedule {InstructorScheduleId}: canEditOwn={CanEditOwn}",
                    LogSanitizer.SanitizeId(user.MothraId), instructorScheduleId, canEdit);

                return Ok(response);
            }
            catch (Exception)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("InstructorScheduleId", instructorScheduleId);
                throw; // Let ApiExceptionFilter handle the response
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
            var user = _userHelper.GetCurrentUser();

            if (user == null)
            {
                _logger.LogWarning("No current user found when getting permission summary");
                return Unauthorized(new { error = "User not authenticated" });
            }

            _logger.LogInformation("Getting permission summary for user {MothraId}", LogSanitizer.SanitizeId(user.MothraId));

            // Enforce elevated permission for full system-wide summary
            var raps = HttpContext.RequestServices.GetRequiredService<RAPSContext>();
            if (!_userHelper.HasPermission(raps, user, ClinicalSchedulePermissions.Manage))
            {
                _logger.LogWarning("User {MothraId} attempted to access full permission summary without manage permission", LogSanitizer.SanitizeId(user.MothraId));
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
                    s.ScheduleEditPermission
                })
                .ToListAsync(HttpContext.RequestAborted);

            var userPermissions = await _permissionService.GetUserServicePermissionsAsync(HttpContext.RequestAborted);

            var servicesWithPermissions = services.Select(s =>
            {
                var ServiceId = s.ServiceId;
                var ServiceName = s.ServiceName;
                var hasCustomPermission = !string.IsNullOrEmpty(s.ScheduleEditPermission);
                var userCanEdit = userPermissions.GetValueOrDefault(s.ServiceId, false);
                return new
                {
                    ServiceId,
                    ServiceName,
                    hasCustomPermission,
                    userCanEdit
                };
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
                    servicesWithCustomPermissions,
                    defaultPermission = ClinicalSchedulePermissions.Manage
                },
                services = servicesWithPermissions
            };

            _logger.LogInformation("Permission summary for user {MothraId}: total={Total}, editable={Editable}, custom={Custom}",
                LogSanitizer.SanitizeId(user.MothraId), services.Count, totalEditableServices, servicesWithCustomPermissions);

            return Ok(response);
        }
    }
}
