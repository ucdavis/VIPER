using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for effort verification operations.
/// Handles self-service verification by instructors and admin email notifications.
/// </summary>
[Route("/api/effort/verification")]
[Permission(Allow = EffortPermissions.AnyViewAccess)]
public class VerificationController : BaseEffortController
{
    private readonly IVerificationService _verificationService;
    private readonly IEffortPermissionService _permissionService;
    private readonly IInstructorService _instructorService;

    public VerificationController(
        IVerificationService verificationService,
        IEffortPermissionService permissionService,
        IInstructorService instructorService,
        ILogger<VerificationController> logger) : base(logger)
    {
        _verificationService = verificationService;
        _permissionService = permissionService;
        _instructorService = instructorService;
    }

    // ====================
    // Self-service endpoints (instructor verifying own effort)
    // ====================

    /// <summary>
    /// Get the current user's effort data for self-service verification.
    /// No special permission required - any authenticated user can view their own effort.
    /// Returns an empty DTO if the user has no instructor record for the term.
    /// </summary>
    [HttpGet("my-effort")]
    public async Task<ActionResult<MyEffortDto>> GetMyEffort(
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        var result = await _verificationService.GetMyEffortAsync(termCode, ct);
        if (result == null)
        {
            // Return empty DTO instead of 404 to avoid global error banner
            return Ok(new MyEffortDto
            {
                Instructor = new PersonDto(),
                EffortRecords = [],
                CrossListedCourses = [],
                HasZeroEffort = false,
                ZeroEffortRecordIds = [],
                CanVerify = false,
                CanEdit = false,
                HasVerifyPermission = false,
                TermName = "",
                LastModifiedDate = null,
                ClinicalAsWeeksStartTermCode = EffortConstants.ClinicalAsWeeksStartTermCode
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Verify the current user's effort for a term.
    /// Sets the EffortVerified timestamp.
    /// </summary>
    [HttpPost("verify")]
    [Permission(Allow = EffortPermissions.VerifyEffort)]
    public async Task<ActionResult<VerificationResult>> VerifyEffort(
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("termCode", termCode);

        var result = await _verificationService.VerifyEffortAsync(termCode, ct);

        // Return 200 with result (including any error details) so frontend can display specific errors
        return Ok(result);
    }

    // ====================
    // Admin endpoints (department admin sending emails)
    // ====================

    /// <summary>
    /// Send a verification email to a specific instructor.
    /// </summary>
    [HttpPost("send-email")]
    [Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
    public async Task<ActionResult<EmailSendResult>> SendVerificationEmail(
        [FromBody] SendVerificationEmailRequest request,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", request.PersonId);
        SetExceptionContext("termCode", request.TermCode);

        // Verify permission to this instructor's department
        var instructor = await _instructorService.GetInstructorAsync(request.PersonId, request.TermCode, ct);
        if (instructor == null)
        {
            return NotFound("Instructor not found.");
        }

        if (!await _permissionService.CanViewDepartmentAsync(instructor.EffortDept, ct))
        {
            return Forbid();
        }

        var result = await _verificationService.SendVerificationEmailAsync(request.PersonId, request.TermCode, ct);

        return Ok(result);
    }

    /// <summary>
    /// Send verification emails to all unverified instructors in a department.
    /// </summary>
    [HttpPost("send-bulk-email")]
    [Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
    public async Task<ActionResult<BulkEmailResult>> SendBulkVerificationEmails(
        [FromBody] SendBulkEmailRequest request,
        CancellationToken ct = default)
    {
        SetExceptionContext("departmentCode", request.DepartmentCode);
        SetExceptionContext("termCode", request.TermCode);

        // Verify permission to this department
        if (!await _permissionService.CanViewDepartmentAsync(request.DepartmentCode, ct))
        {
            return Forbid();
        }

        var result = await _verificationService.SendBulkVerificationEmailsAsync(
            request.DepartmentCode, request.TermCode, ct);

        return Ok(result);
    }

    /// <summary>
    /// Get the email history for an instructor (verification emails sent).
    /// </summary>
    [HttpGet("{personId:int}/email-history")]
    [Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
    public async Task<ActionResult<List<EmailHistoryDto>>> GetEmailHistory(
        int personId,
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        // Verify permission to this instructor's department
        var instructor = await _instructorService.GetInstructorAsync(personId, termCode, ct);
        if (instructor == null)
        {
            return NotFound("Instructor not found.");
        }

        if (!await _permissionService.CanViewDepartmentAsync(instructor.EffortDept, ct))
        {
            return Forbid();
        }

        var result = await _verificationService.GetEmailHistoryAsync(personId, termCode, ct);

        return Ok(result);
    }

    /// <summary>
    /// Check if an instructor can verify their effort (no zero-effort records).
    /// </summary>
    [HttpGet("{personId:int}/can-verify")]
    [Permission(Allow = $"{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments},{EffortPermissions.VerifyEffort}")]
    public async Task<ActionResult<CanVerifyResult>> CanVerify(
        int personId,
        [FromQuery] int termCode,
        CancellationToken ct = default)
    {
        SetExceptionContext("personId", personId);
        SetExceptionContext("termCode", termCode);

        // Allow self-service check for current user
        var currentPersonId = _permissionService.GetCurrentPersonId();
        if (personId != currentPersonId)
        {
            // Verify permission to this instructor's department
            var instructor = await _instructorService.GetInstructorAsync(personId, termCode, ct);
            if (instructor == null)
            {
                return NotFound("Instructor not found.");
            }

            if (!await _permissionService.CanViewDepartmentAsync(instructor.EffortDept, ct))
            {
                return Forbid();
            }
        }

        var result = await _verificationService.CanVerifyAsync(personId, termCode, ct);

        return Ok(result);
    }
}
