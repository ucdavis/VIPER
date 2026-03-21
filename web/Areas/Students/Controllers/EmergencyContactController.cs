using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Students.Controllers;

[Route("/api/students/emergency-contacts")]
public class EmergencyContactController : ApiController
{
    private readonly IEmergencyContactService _service;
    private readonly IEmergencyContactExportService _exportService;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<EmergencyContactController> _logger;

    public EmergencyContactController(
        IEmergencyContactService service,
        IEmergencyContactExportService exportService,
        RAPSContext rapsContext,
        IUserHelper userHelper,
        ILogger<EmergencyContactController> logger)
    {
        _service = service;
        _exportService = exportService;
        _rapsContext = rapsContext;
        _userHelper = userHelper;
        _logger = logger;
    }

    /// <summary>
    /// Get all current DVM students with their emergency contact completeness status.
    /// </summary>
    [HttpGet]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin,SVMSecure.SIS.AllStudents")]
    public async Task<ActionResult<List<StudentContactListItemDto>>> GetStudentContactList()
    {
        var result = await _service.GetStudentContactListAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get full contact detail for a specific student.
    /// Admins, SIS users, and the student themselves can access this endpoint.
    /// </summary>
    [HttpGet("{personId}")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin,SVMSecure.SIS.AllStudents,SVMSecure.Students.EmergencyContactStudent")]
    public async Task<ActionResult<StudentContactDetailDto>> GetStudentContactDetail(int personId)
    {
        var currentUser = _userHelper.GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        // Students can only view their own record
        var isAdmin = _userHelper.HasPermission(_rapsContext, currentUser, EmergencyContactPermissions.Admin);
        var isSis = _userHelper.HasPermission(_rapsContext, currentUser, EmergencyContactPermissions.SISAllStudents);
        if (!isAdmin && !isSis && currentUser.AaudUserId != personId)
        {
            _logger.LogWarning(
                "User {LoginId} attempted to access emergency contact for PersonId {PersonId}",
                LogSanitizer.SanitizeId(currentUser.LoginId),
                LogSanitizer.SanitizeId(personId.ToString()));
            return ForbidApi();
        }

        var canEdit = await _service.CanEditAsync(personId, currentUser.LoginId);
        var result = await _service.GetStudentContactDetailAsync(personId, canEdit);
        if (result == null)
        {
            return NotFound();
        }

        result.IsAdmin = isAdmin || isSis;
        if (!result.IsAdmin)
        {
            result.UpdatedBy = null;
        }
        return Ok(result);
    }

    /// <summary>
    /// Update or create contact information for a student.
    /// Admin can update any student. Students can update their own record when permitted.
    /// Returns the updated detail DTO so the frontend can refresh without a second GET.
    /// </summary>
    [HttpPut("{personId}")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin,SVMSecure.Students.EmergencyContactStudent")]
    public async Task<ActionResult<StudentContactDetailDto>> UpdateStudentContact(int personId, UpdateStudentContactRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var currentUser = _userHelper.GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var canEdit = await _service.CanEditAsync(personId, currentUser.LoginId);
        if (!canEdit)
        {
            _logger.LogWarning(
                "User {LoginId} attempted to update emergency contact for PersonId {PersonId} without permission",
                LogSanitizer.SanitizeId(currentUser.LoginId),
                LogSanitizer.SanitizeId(personId.ToString()));
            return ForbidApi();
        }

        try
        {
            await _service.UpdateStudentContactAsync(personId, request, currentUser.LoginId ?? "unknown");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("PhoneValidation", ex.Message);
            return ValidationProblem(ModelState);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Cannot update emergency contact for PersonId {PersonId}: {Message}",
                LogSanitizer.SanitizeId(personId.ToString()),
                LogSanitizer.SanitizeString(ex.Message));
            return NotFound();
        }

        // Return the refreshed detail so the frontend has updated LastUpdated/UpdatedBy
        var result = await _service.GetStudentContactDetailAsync(personId, canEdit);
        if (result == null)
        {
            return NotFound();
        }

        var isAdmin = _userHelper.HasPermission(_rapsContext, currentUser, EmergencyContactPermissions.Admin);
        var isSis = _userHelper.HasPermission(_rapsContext, currentUser, EmergencyContactPermissions.SISAllStudents);
        result.IsAdmin = isAdmin || isSis;
        if (!result.IsAdmin)
        {
            result.UpdatedBy = null;
        }
        return Ok(result);
    }

    /// <summary>
    /// Get all student contacts formatted for a report.
    /// </summary>
    [HttpGet("report")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin,SVMSecure.SIS.AllStudents")]
    public async Task<ActionResult<List<StudentContactReportDto>>> GetStudentContactReport()
    {
        var result = await _service.GetStudentContactReportAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get current access status: whether the app is open for all students
    /// and which students have individual access grants.
    /// </summary>
    [HttpGet("access/status")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin")]
    public async Task<ActionResult<AppAccessStatusDto>> GetAccessStatus()
    {
        var result = await _service.GetAccessStatusAsync();
        return Ok(result);
    }

    /// <summary>
    /// Toggle app-wide student self-service access on or off.
    /// </summary>
    [HttpPost("access/toggle-app")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin")]
    public async Task<ActionResult<bool>> ToggleAppAccess()
    {
        var newState = await _service.ToggleAppAccessAsync();
        return Ok(newState);
    }

    /// <summary>
    /// Toggle individual student access by PersonId.
    /// </summary>
    [HttpPost("access/{personId}/toggle")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin")]
    public async Task<ActionResult<bool>> ToggleIndividualAccess(int personId)
    {
        try
        {
            var newState = await _service.ToggleIndividualAccessAsync(personId);
            return Ok(newState);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Cannot toggle individual access for PersonId {PersonId}: {Message}",
                LogSanitizer.SanitizeId(personId.ToString()),
                LogSanitizer.SanitizeString(ex.Message));
            return NotFound();
        }
    }

    /// <summary>
    /// Check whether the given student's contact info can be edited by the current user.
    /// </summary>
    [HttpGet("can-edit/{personId}")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin,SVMSecure.SIS.AllStudents,SVMSecure.Students.EmergencyContactStudent")]
    public async Task<ActionResult<bool>> CanEdit(int personId)
    {
        var currentUser = _userHelper.GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var canEdit = await _service.CanEditAsync(personId, currentUser.LoginId);
        return Ok(canEdit);
    }

    /// <summary>
    /// Export all emergency contacts as an Excel file.
    /// </summary>
    [HttpPost("export/excel")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin,SVMSecure.SIS.AllStudents")]
    public async Task<ActionResult> ExportExcel()
    {
        var data = await _service.GetStudentContactReportAsync();
        if (data.Count == 0)
        {
            return NoContent();
        }

        var stream = _exportService.GenerateExcel(data);
        var filename = ExcelHelper.BuildExportFilename(new ExportFilenameOptions
        {
            ReportName = "EmergencyContacts"
        });
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    /// <summary>
    /// Export all emergency contacts as a PDF file.
    /// </summary>
    [HttpPost("export/pdf")]
    [Permission(Allow = "SVMSecure.Students.EmergencyContactAdmin,SVMSecure.SIS.AllStudents")]
    public async Task<ActionResult> ExportPdf()
    {
        var data = await _service.GetStudentContactReportAsync();
        if (data.Count == 0)
        {
            return NoContent();
        }

        var pdfBytes = _exportService.GeneratePdf(data);
        return File(pdfBytes, "application/pdf", $"EmergencyContacts_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
