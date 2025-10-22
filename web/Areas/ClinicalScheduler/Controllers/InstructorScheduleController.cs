using Microsoft.AspNetCore.Mvc;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.ClinicalScheduler.Extensions;
using Viper.Areas.ClinicalScheduler.Constants;
using Viper.Areas.ClinicalScheduler.Models;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Requests;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Areas.ClinicalScheduler.Validators;
using Viper.Classes.SQLContext;
using Viper.Models.ClinicalScheduler;
using Web.Authorization;
using VIPER.Areas.ClinicalScheduler.Utilities;

namespace Viper.Areas.ClinicalScheduler.Controllers
{

    /// <summary>
    /// Controller for managing instructor schedule assignments
    /// </summary>
    [Route("api/clinicalscheduler/instructor-schedules")]
    [ApiController]
    [Permission(Allow = ClinicalSchedulePermissions.Base)]
    public class InstructorScheduleController : BaseClinicalSchedulerController
    {
        private readonly IScheduleEditService _scheduleEditService;
        private readonly IScheduleAuditService _auditService;
        private readonly ISchedulePermissionService _permissionService;
        private readonly IUserHelper _userHelper;
        private readonly AddInstructorValidator _validator;

        public InstructorScheduleController(
            IScheduleEditService scheduleEditService,
            IScheduleAuditService auditService,
            ISchedulePermissionService permissionService,
            IUserHelper userHelper,
            IGradYearService gradYearService,
            ILogger<InstructorScheduleController> logger,
            AddInstructorValidator validator)
            : base(gradYearService, logger)
        {
            _scheduleEditService = scheduleEditService;
            _auditService = auditService;
            _permissionService = permissionService;
            _userHelper = userHelper;
            _validator = validator;
        }

        /// <summary>
        /// Add an instructor to specific rotation weeks
        /// </summary>
        /// <param name="request">Add instructor request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response containing created instructor schedule entries and optional warning</returns>
        [HttpPost]
        [ProducesResponseType(typeof(AddInstructorResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ErrorResponse), 409)] // Conflict
        public async Task<IActionResult> AddInstructor(
            [FromBody] AddInstructorRequest request,
            CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                // Step 1: Validate request
                var validationResult = await _validator.ValidateRequestAsync(request, correlationId, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.ValidationError,
                        validationResult.ErrorMessage!,
                        correlationId));
                }

                // Step 2: Check permissions - include own schedule check
                if (!await CheckPermissionsForAddAsync(request.RotationId!.Value, request.MothraId!, correlationId, cancellationToken))
                {
                    return Forbid();
                }

                // Step 3: Check for conflicts and build warning message
                var warningMessage = await BuildConflictWarningAsync(
                    request.MothraId!,
                    request.WeekIds,
                    request.GradYear!.Value,
                    request.RotationId!.Value,
                    correlationId,
                    cancellationToken);

                // Step 4: Add instructor through service layer
                var response = await ProcessAddInstructorAsync(
                    request,
                    warningMessage,
                    correlationId,
                    cancellationToken);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return HandleUnauthorizedAccess(ex, request.RotationId!.Value, correlationId);
            }
            catch (InvalidOperationException ex)
            {
                return HandleInvalidOperation(ex, correlationId);
            }
            catch (Exception ex)
            {
                return HandleSystemError(ex, request.RotationId!.Value, correlationId);
            }
        }

        private async Task<bool> CheckPermissionsForAddAsync(
            int rotationId,
            string targetMothraId,
            string correlationId,
            CancellationToken cancellationToken)
        {
            // First check regular rotation edit permissions
            if (await _permissionService.HasEditPermissionForRotationAsync(rotationId, cancellationToken))
            {
                return true;
            }

            // Check if user has EditOwnSchedule permission and is adding themselves
            var currentUser = _userHelper.GetCurrentUser();
            if (currentUser != null)
            {
                // Check if user has EditOwnSchedule permission
                var rapsContext = HttpContext.RequestServices.GetService<RAPSContext>();
                if (_userHelper.HasPermission(rapsContext, currentUser, ClinicalSchedulePermissions.EditOwnSchedule) &&
                    currentUser.MothraId.Equals(targetMothraId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("User {MothraId} is adding their own schedule with EditOwnSchedule permission (CorrelationId: {CorrelationId})",
                        LogSanitizer.SanitizeId(currentUser.MothraId), correlationId);
                    return true;
                }
            }

            _logger.LogWarning("User attempted to add instructor {TargetMothraId} to rotation {RotationId} without permission (CorrelationId: {CorrelationId})",
                LogSanitizer.SanitizeId(targetMothraId), rotationId, correlationId);
            return false;
        }

        private async Task<string?> BuildConflictWarningAsync(
            string mothraId,
            int[] weekIds,
            int gradYear,
            int rotationId,
            string correlationId,
            CancellationToken cancellationToken)
        {
            var otherRotations = await _scheduleEditService.GetOtherRotationSchedulesAsync(
                mothraId, weekIds, gradYear, rotationId, cancellationToken);

            if (!otherRotations.Any())
                return null;

            // Get unique rotation names
            var rotationNames = otherRotations
                .Select(s => s.Rotation?.Name ?? $"Rotation {s.RotationId}")
                .Distinct()
                .OrderBy(name => name);

            var warningMessage = $"Note: This instructor is also scheduled for {string.Join(", ", rotationNames)}";

            _logger.LogInformation("Instructor {MothraId} has other rotation assignments (CorrelationId: {CorrelationId}): {Details}",
                LogSanitizer.SanitizeId(mothraId), correlationId, warningMessage);

            return warningMessage;
        }

        private async Task<AddInstructorResponse> ProcessAddInstructorAsync(
            AddInstructorRequest request,
            string? warningMessage,
            string correlationId,
            CancellationToken cancellationToken)
        {
            // Add instructor to schedule
            var createdSchedules = await _scheduleEditService.AddInstructorAsync(
                request.MothraId!,
                request.RotationId!.Value,
                request.WeekIds,
                request.GradYear!.Value,
                request.IsPrimaryEvaluator,
                cancellationToken);

            var scheduleResponses = new List<InstructorScheduleResponse>();
            foreach (var schedule in createdSchedules)
            {
                var canRemove = await _scheduleEditService.CanRemoveInstructorAsync(
                    schedule.InstructorScheduleId, cancellationToken);
                scheduleResponses.Add(schedule.ToResponse(canRemove));
            }

            _logger.LogInformation("Successfully added instructor to rotation {RotationId} for {WeekCount} weeks (CorrelationId: {CorrelationId})",
                request.RotationId!.Value, request.WeekIds.Length, correlationId);

            return new AddInstructorResponse
            {
                Schedules = scheduleResponses,
                ScheduleIds = scheduleResponses.Select(s => s.InstructorScheduleId).ToList(),
                WarningMessage = warningMessage
            };
        }

        private IActionResult HandleUnauthorizedAccess(
            UnauthorizedAccessException ex,
            int rotationId,
            string correlationId)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to add instructor to rotation {RotationId} (CorrelationId: {CorrelationId})",
                rotationId, correlationId);
            return Forbid();
        }

        private IActionResult HandleInvalidOperation(
            InvalidOperationException ex,
            string correlationId)
        {
            _logger.LogWarning(ex, "Invalid operation (CorrelationId: {CorrelationId}): {ErrorMessage}",
                correlationId, ex.Message);

            // Map specific error messages to user-friendly messages
            string userMessage = UserMessages.GenericError;
            string errorCode = ErrorCodes.InvalidOperation;

            if (ex.Message.Contains("Person with MothraId") && ex.Message.Contains("not found"))
            {
                userMessage = UserMessages.InvalidPerson;
                errorCode = ErrorCodes.ResourceNotFound;
            }
            else if (ex.Message.Contains("Rotation with ID") && ex.Message.Contains("not found"))
            {
                userMessage = UserMessages.InvalidRotation;
                errorCode = ErrorCodes.ResourceNotFound;
            }
            else if (ex.Message.Contains("Week(s) not found"))
            {
                userMessage = UserMessages.InvalidWeeks;
                errorCode = ErrorCodes.ResourceNotFound;
            }
            else if (ex.Message.Contains("already scheduled") || ex.Message.Contains("duplicate"))
            {
                userMessage = UserMessages.DuplicateSchedule;
                errorCode = ErrorCodes.DuplicateSchedule;
            }

            return BadRequest(new ErrorResponse(errorCode, userMessage, correlationId));
        }

        private IActionResult HandleSystemError(
            Exception ex,
            int rotationId,
            string correlationId)
        {
            _logger.LogError(ex, "Error adding instructor to rotation {RotationId} (CorrelationId: {CorrelationId})",
                rotationId, correlationId);
            return StatusCode(500, new ErrorResponse(
                ErrorCodes.SystemError,
                UserMessages.GenericError,
                correlationId));
        }

        /// <summary>
        /// Remove an instructor from a schedule assignment
        /// </summary>
        /// <param name="instructorScheduleId">Instructor schedule ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>200 OK with RemoveInstructorResponse containing removal details, or error response</returns>
        [HttpDelete("{instructorScheduleId:int}")]
        [ProducesResponseType(typeof(RemoveInstructorResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> RemoveInstructor(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                // Validate input parameters
                if (instructorScheduleId <= 0)
                {
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.ValidationError,
                        ValidationMessages.InstructorScheduleIdRequired,
                        correlationId));
                }

                var (success, wasPrimaryEvaluator, instructorName) = await _scheduleEditService.RemoveInstructorScheduleAsync(instructorScheduleId, cancellationToken);

                if (!success)
                {
                    return NotFound(new ErrorResponse(
                        ErrorCodes.ResourceNotFound,
                        "The specified schedule was not found.",
                        correlationId));
                }

                _logger.LogInformation("Successfully removed instructor schedule {ScheduleId} (Primary: {WasPrimary}) (CorrelationId: {CorrelationId})",
                    instructorScheduleId, wasPrimaryEvaluator, correlationId);

                return Ok(new RemoveInstructorResponse
                {
                    Success = true,
                    WasPrimaryEvaluator = wasPrimaryEvaluator,
                    InstructorName = instructorName
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to remove instructor schedule {ScheduleId} (CorrelationId: {CorrelationId})",
                    instructorScheduleId, correlationId);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when removing instructor schedule {ScheduleId} (CorrelationId: {CorrelationId}): {ErrorMessage}",
                    instructorScheduleId, correlationId, ex.Message);

                // Map to user-friendly message
                string userMessage = "Cannot remove this instructor at this time. Please try again later.";
                if (ex.Message.Contains("primary evaluator"))
                {
                    userMessage = "Cannot remove the primary evaluator when they are the only instructor assigned.";
                }

                return BadRequest(new ErrorResponse(
                    ErrorCodes.InvalidOperation,
                    userMessage,
                    correlationId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing instructor schedule {ScheduleId} (CorrelationId: {CorrelationId})",
                    instructorScheduleId, correlationId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.SystemError,
                    UserMessages.GenericError,
                    correlationId));
            }
        }

        /// <summary>
        /// Set or unset an instructor as primary evaluator
        /// </summary>
        /// <param name="instructorScheduleId">Instructor schedule ID</param>
        /// <param name="request">Primary evaluator request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success or failure result</returns>
        [HttpPut("{instructorScheduleId:int}/primary")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> SetPrimaryEvaluator(
            int instructorScheduleId,
            [FromBody] SetPrimaryEvaluatorRequest request,
            CancellationToken cancellationToken = default)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model validation failed (CorrelationId: {CorrelationId}): {Errors}",
                        correlationId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.ValidationError,
                        "Please check your input and try again.",
                        correlationId));
                }

                // Validate input parameters
                if (instructorScheduleId <= 0)
                {
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.ValidationError,
                        ValidationMessages.InstructorScheduleIdRequired,
                        correlationId));
                }

                var (success, previousPrimaryName) = await _scheduleEditService.SetPrimaryEvaluatorAsync(
                    instructorScheduleId, request.IsPrimary!.Value, cancellationToken, request.RequiresPrimaryEvaluator);

                if (!success)
                {
                    return NotFound(new ErrorResponse(
                        ErrorCodes.ResourceNotFound,
                        "The specified schedule was not found.",
                        correlationId));
                }

                var action = request.IsPrimary!.Value ? "set as" : "removed as";
                _logger.LogInformation("Successfully {Action} primary evaluator for instructor schedule {ScheduleId} (CorrelationId: {CorrelationId})",
                    action, instructorScheduleId, correlationId);

                return Ok(new
                {
                    message = $"Instructor successfully {action} primary evaluator",
                    isPrimaryEvaluator = request.IsPrimary!.Value,
                    previousPrimaryName = previousPrimaryName
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to modify primary evaluator for instructor schedule {ScheduleId} (CorrelationId: {CorrelationId})",
                    instructorScheduleId, correlationId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary evaluator for instructor schedule {ScheduleId} (CorrelationId: {CorrelationId})",
                    instructorScheduleId, correlationId);
                return StatusCode(500, new ErrorResponse(
                    ErrorCodes.SystemError,
                    UserMessages.GenericError,
                    correlationId));
            }
        }

        /// <summary>
        /// Check for schedule conflicts for an instructor
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="weekIds">Comma-separated week IDs</param>
        /// <param name="gradYear">Graduation year</param>
        /// <param name="excludeRotationId">Optional rotation ID to exclude from conflict check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Other rotation assignments for the instructor (informational)</returns>
        [HttpGet("other-assignments")]
        [ProducesResponseType(typeof(ScheduleConflictResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CheckScheduleConflicts(
            [FromQuery] string mothraId,
            [FromQuery] string weekIds,
            [FromQuery] int gradYear,
            [FromQuery] int? excludeRotationId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(mothraId))
                {
                    return BadRequest(ValidationMessages.MothraIdRequired);
                }

                if (string.IsNullOrEmpty(weekIds))
                {
                    return BadRequest(ValidationMessages.WeekIdsRequiredForConflicts);
                }

                // Validate gradYear
                if (gradYear <= 0)
                {
                    return BadRequest(new ErrorResponse(
                        ErrorCodes.ValidationError,
                        "GradYear must be greater than zero.",
                        Guid.NewGuid().ToString()));
                }

                // Parse week IDs and track invalid ones
                var weekIdTokens = weekIds.Split(',').Select(id => id.Trim()).ToArray();
                var parsedWeekIds = new List<int>();
                var invalidTokens = new List<string>();

                foreach (var token in weekIdTokens)
                {
                    if (int.TryParse(token, out var weekId) && weekId > 0)
                    {
                        parsedWeekIds.Add(weekId);
                    }
                    else
                    {
                        invalidTokens.Add(token);
                    }
                }

                if (invalidTokens.Any())
                {
                    return BadRequest($"Invalid week IDs found: {string.Join(", ", invalidTokens)}. All week IDs must be positive integers.");
                }

                if (!parsedWeekIds.Any())
                {
                    return BadRequest(ValidationMessages.AtLeastOneValidWeekId);
                }

                var weekIdArray = parsedWeekIds.Distinct().ToArray();

                var otherRotations = await _scheduleEditService.GetOtherRotationSchedulesAsync(
                    mothraId, weekIdArray, gradYear, excludeRotationId, cancellationToken);

                var response = new ScheduleConflictResponse
                {
                    Conflicts = otherRotations.Select(c => c.ToResponse()).ToList(),
                    Message = otherRotations.Any()
                        ? $"Note: Instructor {mothraId} is also scheduled for {otherRotations.Count} other rotation(s) during these weeks"
                        : $"No other rotation assignments found for instructor {mothraId}"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule conflicts for {MothraId}", LogSanitizer.SanitizeId(mothraId));
                return StatusCode(500, "An error occurred while checking for schedule conflicts");
            }
        }

        /// <summary>
        /// Get audit history for an instructor schedule
        /// </summary>
        /// <param name="instructorScheduleId">Instructor schedule ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of audit entries</returns>
        [HttpGet("{instructorScheduleId:int}/audit")]
        [ProducesResponseType(typeof(List<ScheduleAudit>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAuditHistory(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var auditHistory = await _auditService.GetInstructorScheduleAuditHistoryAsync(instructorScheduleId, cancellationToken);
                return Ok(auditHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit history for instructor schedule {ScheduleId}", instructorScheduleId);
                return StatusCode(500, "An error occurred while retrieving audit history");
            }
        }

    }
}
