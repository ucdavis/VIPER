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
        /// <returns>Object with canEdit boolean indicating if user has permission</returns>
        /// <remarks>
        /// Protected by authentication and [Permission] attribute.
        /// Returns only the permission boolean to minimize information disclosure.
        /// </remarks>
        [HttpGet("service/{serviceId:int}/can-edit")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CanEditService(int serviceId)
        {
            if (serviceId <= 0)
            {
                _logger.LogWarning("Invalid service ID provided for permission check: {ServiceId}", serviceId);
                return BadRequest(new { error = "Service ID must be a positive integer" });
            }

            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when checking service edit permission for service {ServiceId}", serviceId);
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Checking edit permission for user {MothraId} and service {ServiceId}", LogSanitizer.SanitizeId(user.MothraId), serviceId);

                // Verify service existence before permission check to prevent information disclosure
                var service = await _csContext.Services
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ServiceId == serviceId, HttpContext.RequestAborted);

                if (service == null)
                {
                    _logger.LogWarning("Service {ServiceId} not found for user {MothraId}", serviceId, LogSanitizer.SanitizeId(user.MothraId));
                    return Ok(new { canEdit = false });
                }

                // Check edit permissions for authenticated user
                var canEdit = await _permissionService.HasEditPermissionForServiceAsync(serviceId, HttpContext.RequestAborted);

                _logger.LogInformation("Permission check result for user {MothraId} and service {ServiceId}: canEdit={CanEdit}",
                    LogSanitizer.SanitizeId(user.MothraId), serviceId, canEdit);

                return Ok(new { canEdit });
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
        /// <returns>Object with canEdit boolean indicating if user has permission</returns>
        /// <remarks>
        /// Permission is determined by the rotation's associated service.
        /// Protected by authentication and [Permission] attribute.
        /// Returns only the permission boolean to minimize information disclosure.
        /// </remarks>
        [HttpGet("rotation/{rotationId:int}/can-edit")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CanEditRotation(int rotationId)
        {
            if (rotationId <= 0)
            {
                _logger.LogWarning("Invalid rotation ID provided for permission check: {RotationId}", rotationId);
                return BadRequest(new { error = "Rotation ID must be a positive integer" });
            }

            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when checking rotation edit permission for rotation {RotationId}", rotationId);
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Checking edit permission for user {MothraId} and rotation {RotationId}", LogSanitizer.SanitizeId(user.MothraId), rotationId);

                // Verify rotation existence before permission check to prevent information disclosure
                var rotation = await _csContext.Rotations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RotId == rotationId, HttpContext.RequestAborted);

                if (rotation == null)
                {
                    _logger.LogWarning("Rotation {RotationId} not found for user {MothraId}", rotationId, LogSanitizer.SanitizeId(user.MothraId));
                    return Ok(new { canEdit = false });
                }

                // Check edit permissions for authenticated user
                var canEdit = await _permissionService.HasEditPermissionForRotationAsync(rotationId, HttpContext.RequestAborted);

                _logger.LogInformation("Permission check result for user {MothraId} and rotation {RotationId}: canEdit={CanEdit}",
                    LogSanitizer.SanitizeId(user.MothraId), rotationId, canEdit);

                return Ok(new { canEdit });
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
        /// <returns>Object with canEditOwn boolean indicating if user can edit their own schedule</returns>
        /// <remarks>
        /// Requires EditOwnSchedule permission and the schedule must belong to the current user.
        /// Protected by authentication and [Permission] attribute.
        /// Returns only the permission boolean to minimize information disclosure.
        /// </remarks>
        [HttpGet("instructor-schedule/{instructorScheduleId:int}/can-edit-own")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<object>> CanEditOwnSchedule(int instructorScheduleId)
        {
            if (instructorScheduleId <= 0)
            {
                _logger.LogWarning("Invalid instructor schedule ID provided for own schedule permission check: {InstructorScheduleId}", instructorScheduleId);
                return BadRequest(new { error = "Instructor schedule ID must be a positive integer" });
            }

            try
            {
                var user = _userHelper.GetCurrentUser();

                if (user == null)
                {
                    _logger.LogWarning("No current user found when checking own schedule edit permission for instructor schedule {InstructorScheduleId}", instructorScheduleId);
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("Checking own schedule edit permission for user {MothraId} and instructor schedule {InstructorScheduleId}", LogSanitizer.SanitizeId(user.MothraId), instructorScheduleId);

                // Verify schedule existence before permission check to prevent information disclosure
                var schedule = await _csContext.InstructorSchedules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.InstructorScheduleId == instructorScheduleId, HttpContext.RequestAborted);

                if (schedule == null)
                {
                    _logger.LogWarning("Instructor schedule {InstructorScheduleId} not found for user {MothraId}", instructorScheduleId, LogSanitizer.SanitizeId(user.MothraId));
                    return Ok(new { canEditOwn = false });
                }

                // Check own schedule edit permissions for authenticated user
                var canEditOwn = await _permissionService.CanEditOwnScheduleAsync(instructorScheduleId, HttpContext.RequestAborted);

                _logger.LogDebug("Own schedule permission check result for user {MothraId} and instructor schedule {InstructorScheduleId}: canEditOwn={CanEditOwn}",
                    LogSanitizer.SanitizeId(user.MothraId), instructorScheduleId, canEditOwn);

                return Ok(new { canEditOwn });
            }
            catch (Exception)
            {
                // Store context for ApiExceptionFilter to use in logging
                SetExceptionContext("InstructorScheduleId", instructorScheduleId);
                throw; // Let ApiExceptionFilter handle the response
            }
        }

    }
}
