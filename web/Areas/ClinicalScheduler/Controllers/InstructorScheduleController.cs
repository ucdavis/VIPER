using Microsoft.AspNetCore.Mvc;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Areas.ClinicalScheduler.Extensions;
using Viper.Areas.ClinicalScheduler.Constants;
using Viper.Models.ClinicalScheduler;
using Web.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    /// <summary>
    /// DTOs for instructor schedule operations
    /// </summary>
    public class AddInstructorRequest
    {
        [Required]
        public string MothraId { get; set; } = string.Empty;

        [Required]
        public int RotationId { get; set; }

        [Required]
        public int[] WeekIds { get; set; } = Array.Empty<int>();

        public bool IsPrimaryEvaluator { get; set; } = false;
    }

    public class SetPrimaryEvaluatorRequest
    {
        [Required]
        public bool IsPrimary { get; set; }
    }

    public class InstructorScheduleResponse
    {
        public int InstructorScheduleId { get; set; }
        public string MothraId { get; set; } = string.Empty;
        public int RotationId { get; set; }
        public int WeekId { get; set; }
        public bool IsPrimaryEvaluator { get; set; }
        public bool CanRemove { get; set; }
    }

    public class ScheduleConflictResponse
    {
        public List<InstructorScheduleResponse> Conflicts { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public bool HasConflicts => Conflicts.Any();
    }

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

        public InstructorScheduleController(
            IScheduleEditService scheduleEditService,
            IScheduleAuditService auditService,
            ISchedulePermissionService permissionService,
            IGradYearService gradYearService,
            ILogger<InstructorScheduleController> logger)
            : base(gradYearService, logger)
        {
            _scheduleEditService = scheduleEditService;
            _auditService = auditService;
            _permissionService = permissionService;
        }

        /// <summary>
        /// Add an instructor to specific rotation weeks
        /// </summary>
        /// <param name="request">Add instructor request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of created instructor schedule entries</returns>
        [HttpPost]
        [ProducesResponseType(typeof(List<InstructorScheduleResponse>), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(string), 409)] // Conflict
        public async Task<IActionResult> AddInstructor(
            [FromBody] AddInstructorRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (!request.WeekIds.Any())
                {
                    return BadRequest(ValidationMessages.WeekIdsRequired);
                }

                // Trim and validate MothraId
                request.MothraId = request.MothraId?.Trim();
                if (string.IsNullOrEmpty(request.MothraId))
                {
                    return BadRequest(ValidationMessages.MothraIdRequired);
                }

                // Validate input parameters
                if (request.RotationId <= 0)
                {
                    return BadRequest(ValidationMessages.RotationIdRequired);
                }

                if (request.WeekIds.Any(w => w <= 0))
                {
                    return BadRequest(ValidationMessages.InvalidWeekIds);
                }

                if (request.WeekIds.Distinct().Count() != request.WeekIds.Length)
                {
                    return BadRequest(ValidationMessages.WeekIdsMustBeUnique);
                }

                // Defense-in-depth: Check permissions at controller level before proceeding
                if (!await _permissionService.HasEditPermissionForRotationAsync(request.RotationId, cancellationToken))
                {
                    _logger.LogWarning("User attempted to add instructor to rotation {RotationId} without permission",
                        request.RotationId);
                    return Forbid("You do not have permission to edit this rotation");
                }

                // Check for conflicts first
                var conflicts = await _scheduleEditService.GetScheduleConflictsAsync(
                    request.MothraId, request.WeekIds, request.RotationId, cancellationToken);

                if (conflicts.Any())
                {
                    var conflictResponse = new ScheduleConflictResponse
                    {
                        Conflicts = conflicts.Select(c => c.ToResponse()).ToList(),
                        Message = $"Instructor {request.MothraId} has scheduling conflicts on the specified weeks"
                    };
                    return Conflict(conflictResponse);
                }

                // Add instructor to schedule
                var createdSchedules = await _scheduleEditService.AddInstructorAsync(
                    request.MothraId,
                    request.RotationId,
                    request.WeekIds,
                    request.IsPrimaryEvaluator,
                    cancellationToken);

                var response = new List<InstructorScheduleResponse>();
                foreach (var schedule in createdSchedules)
                {
                    var canRemove = await _scheduleEditService.CanRemoveInstructorAsync(schedule.InstructorScheduleId, cancellationToken);
                    response.Add(schedule.ToResponse(canRemove));
                }

                _logger.LogInformation("Successfully added instructor to rotation {RotationId} for {WeekCount} weeks",
                    request.RotationId, request.WeekIds.Length);

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to add instructor to rotation {RotationId}",
                    request.RotationId);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when adding instructor to rotation {RotationId}: {ErrorMessage}",
                    request.RotationId, ex.Message);
                return BadRequest("Unable to add instructor due to scheduling constraints");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding instructor to rotation {RotationId}",
                    request.RotationId);
                return StatusCode(500, "An error occurred while adding the instructor to the schedule");
            }
        }

        /// <summary>
        /// Remove an instructor from a schedule assignment
        /// </summary>
        /// <param name="instructorScheduleId">Instructor schedule ID to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success or failure result</returns>
        [HttpDelete("{instructorScheduleId:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RemoveInstructor(
            int instructorScheduleId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input parameters
                if (instructorScheduleId <= 0)
                {
                    return BadRequest(ValidationMessages.InstructorScheduleIdRequired);
                }

                var success = await _scheduleEditService.RemoveInstructorScheduleAsync(instructorScheduleId, cancellationToken);

                if (!success)
                {
                    return NotFound($"Instructor schedule {instructorScheduleId} not found");
                }

                _logger.LogInformation("Successfully removed instructor schedule {ScheduleId}", instructorScheduleId);
                return Ok(new { message = "Instructor successfully removed from schedule" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to remove instructor schedule {ScheduleId}", instructorScheduleId);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when removing instructor schedule {ScheduleId}: {ErrorMessage}", instructorScheduleId, ex.Message);
                return BadRequest("Cannot remove this instructor. They may be the primary evaluator with no other instructors assigned to this rotation/week.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing instructor schedule {ScheduleId}", instructorScheduleId);
                return StatusCode(500, "An error occurred while removing the instructor from the schedule");
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
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetPrimaryEvaluator(
            int instructorScheduleId,
            [FromBody] SetPrimaryEvaluatorRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate input parameters
                if (instructorScheduleId <= 0)
                {
                    return BadRequest(ValidationMessages.InstructorScheduleIdRequired);
                }

                var success = await _scheduleEditService.SetPrimaryEvaluatorAsync(
                    instructorScheduleId, request.IsPrimary, cancellationToken);

                if (!success)
                {
                    return NotFound($"Instructor schedule {instructorScheduleId} not found");
                }

                var action = request.IsPrimary ? "set as" : "removed as";
                _logger.LogInformation("Successfully {Action} primary evaluator for instructor schedule {ScheduleId}",
                    action, instructorScheduleId);

                return Ok(new
                {
                    message = $"Instructor successfully {action} primary evaluator",
                    isPrimaryEvaluator = request.IsPrimary
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to modify primary evaluator for instructor schedule {ScheduleId}", instructorScheduleId);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary evaluator for instructor schedule {ScheduleId}", instructorScheduleId);
                return StatusCode(500, "An error occurred while updating the primary evaluator status");
            }
        }

        /// <summary>
        /// Check for schedule conflicts for an instructor
        /// </summary>
        /// <param name="mothraId">Instructor's MothraID</param>
        /// <param name="weekIds">Comma-separated week IDs</param>
        /// <param name="excludeRotationId">Optional rotation ID to exclude from conflict check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Schedule conflicts if any</returns>
        [HttpGet("conflicts")]
        [ProducesResponseType(typeof(ScheduleConflictResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CheckScheduleConflicts(
            [FromQuery] string mothraId,
            [FromQuery] string weekIds,
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

                var conflicts = await _scheduleEditService.GetScheduleConflictsAsync(
                    mothraId, weekIdArray, excludeRotationId, cancellationToken);

                var response = new ScheduleConflictResponse
                {
                    Conflicts = conflicts.Select(c => c.ToResponse()).ToList(),
                    Message = conflicts.Any()
                        ? $"Found {conflicts.Count} scheduling conflicts for instructor {mothraId}"
                        : $"No scheduling conflicts found for instructor {mothraId}"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule conflicts for {MothraId}", mothraId);
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