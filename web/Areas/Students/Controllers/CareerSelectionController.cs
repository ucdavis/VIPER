using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Students.Constants;
using Viper.Areas.Students.Models;
using Viper.Areas.Students.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Students.Controllers;

[Route("/api/students/career-selection")]
public class CareerSelectionController : ApiController
{
    private readonly ICareerSelectionService _service;
    private readonly ICareerSelectionOptionService _optionService;
    private readonly ICareerSelectionExportService _exportService;
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<CareerSelectionController> _logger;

    public CareerSelectionController(
        ICareerSelectionService service,
        ICareerSelectionOptionService optionService,
        ICareerSelectionExportService exportService,
        RAPSContext rapsContext,
        IUserHelper userHelper,
        ILogger<CareerSelectionController> logger)
    {
        _service = service;
        _optionService = optionService;
        _exportService = exportService;
        _rapsContext = rapsContext;
        _userHelper = userHelper;
        _logger = logger;
    }

    /// <summary>
    /// Get every option of one dropdown type, catch-all last. The type is the URL slug: career,
    /// species or post-grad. Serves both the career selection form and the admin page that
    /// manages the lists; only admins get usage counts, which is needed only for managing options.
    /// </summary>
    [HttpGet("options/{type}")]
    [Permission(Allow = CareerSelectionPermissions.Editors)]
    public async Task<ActionResult<List<CareerSelectionOptionDto>>> GetOptions(string type, CancellationToken ct = default)
    {
        if (!CareerOptionTypes.TryParseSlug(type, out var optionType))
        {
            return NotFound();
        }

        var currentUser = _userHelper.GetCurrentUser();
        var isAdmin = currentUser != null
            && _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Admin);

        var result = await _optionService.GetOptionsAsync(optionType, includeUsage: isAdmin, ct);
        return Ok(result);
    }

    /// <summary>
    /// Add an option to one dropdown. Names are trimmed and must be unique within the dropdown,
    /// ignoring case.
    /// </summary>
    [HttpPost("options/{type}")]
    [Permission(Allow = CareerSelectionPermissions.Admin)]
    public async Task<ActionResult<CareerSelectionOptionDto>> CreateOption(string type, [FromBody] CareerSelectionOptionRequest request, CancellationToken ct = default)
    {
        if (!CareerOptionTypes.TryParseSlug(type, out var optionType))
        {
            return NotFound();
        }

        var result = await _optionService.CreateOptionAsync(optionType, request.Label, ct);
        if (result.Status == CareerSelectionOptionWriteStatus.Success)
        {
            _logger.LogInformation("Career selection {OptionType} option {OptionId} added: {Label}",
                optionType, result.Option!.Id, LogSanitizer.SanitizeString(result.Option.Label));
            return CreatedAtAction(nameof(GetOptions), new { type }, result.Option);
        }
        return OptionWriteFailure(result);
    }

    /// <summary>
    /// Rename an option. The catch-all option cannot be renamed.
    /// </summary>
    [HttpPut("options/{type}/{id:int}")]
    [Permission(Allow = CareerSelectionPermissions.Admin)]
    public async Task<ActionResult<CareerSelectionOptionDto>> UpdateOption(string type, int id, [FromBody] CareerSelectionOptionRequest request, CancellationToken ct = default)
    {
        if (!CareerOptionTypes.TryParseSlug(type, out var optionType))
        {
            return NotFound();
        }

        var result = await _optionService.UpdateOptionAsync(optionType, id, request.Label, ct);
        if (result.Status == CareerSelectionOptionWriteStatus.Success)
        {
            _logger.LogInformation("Career selection {OptionType} option {OptionId} renamed: {Label}",
                optionType, id, LogSanitizer.SanitizeString(result.Option!.Label));
            return Ok(result.Option);
        }
        return OptionWriteFailure(result);
    }

    /// <summary>
    /// Delete an option. Refused for the catch-all option, and for any option a career selection
    /// uses, since the selections hold a foreign key to it.
    /// </summary>
    [HttpDelete("options/{type}/{id:int}")]
    [Permission(Allow = CareerSelectionPermissions.Admin)]
    public async Task<ActionResult> DeleteOption(string type, int id, CancellationToken ct = default)
    {
        if (!CareerOptionTypes.TryParseSlug(type, out var optionType))
        {
            return NotFound();
        }

        var result = await _optionService.DeleteOptionAsync(optionType, id, ct);
        if (result.Status == CareerSelectionOptionWriteStatus.Success)
        {
            _logger.LogInformation("Career selection {OptionType} option {OptionId} deleted", optionType, id);
            return NoContent();
        }
        return OptionWriteFailure(result);
    }

    // The error strings become the response's errorMessage, which the manage page shows as-is.
    private ActionResult OptionWriteFailure(CareerSelectionOptionWriteResult result) => result.Status switch
    {
        CareerSelectionOptionWriteStatus.NotFound => NotFound("The option no longer exists. Reload the page and try again."),
        CareerSelectionOptionWriteStatus.Conflict => Conflict(result.Error),
        _ => BadRequest(result.Error),
    };

    /// <summary>
    /// Search current SVM affiliates for the mentor picker. Admin only: the mentor is
    /// admin-managed and read-only to everyone else, so nobody else needs to search for one.
    /// </summary>
    [HttpGet("mentors")]
    [Permission(Allow = CareerSelectionPermissions.Admin)]
    public async Task<ActionResult<List<MentorOptionDto>>> SearchMentors(string search, CancellationToken ct = default)
    {
        var result = await _service.SearchMentorsAsync(search, ct);
        return Ok(result);
    }

    /// <summary>
    /// Which students the caller may be shown in a roster, resolved once here rather than
    /// separately at each roster endpoint.
    /// </summary>
    private StudentListAccess ResolveStudentListAccess()
    {
        var currentUser = _userHelper.GetCurrentUser();
        if (currentUser == null)
        {
            return StudentListAccess.Denied;
        }

        return _service.ResolveScope(currentUser) switch
        {
            CareerSelectionScope.All => StudentListAccess.AllStudents,
            CareerSelectionScope.Mentored => StudentListAccess.MentoredBy(currentUser.MothraId),
            // A student has a record but no roster: their own page is reached directly.
            _ => StudentListAccess.Denied
        };
    }

    /// <summary>
    /// Get all current DVM students with their career selection completeness status.
    /// A faculty mentor sees only the students they mentor.
    /// </summary>
    [HttpGet]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public async Task<ActionResult<List<StudentCareerListItemDto>>> GetStudentCareerList()
    {
        var access = ResolveStudentListAccess();
        if (access.IsDenied)
        {
            return ForbidApi();
        }

        var result = await _service.GetStudentCareerListAsync(access);
        return Ok(result);
    }

    /// <summary>
    /// Get career selection details for a specific student.
    /// The attribute admits anyone holding one of
    /// <see cref="CareerSelectionPermissions.RecordViewers"/>. Which record each of them may
    /// open is decided below. Admins and read-only viewers may open any record.
    /// Faculty mentors may open the records of their mentees.
    /// Everyone else with app access is limited to their own.
    /// </summary>
    [HttpGet("{personId:int}")]
    [Permission(Allow = CareerSelectionPermissions.RecordViewers)]
    public async Task<ActionResult<StudentCareerDetailDto>> GetStudentCareerDetail(int personId)
    {
        var currentUser = _userHelper.GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var isAdmin = _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Admin);
        var isReadOnly = _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.ReadOnly);
        var isStudent = _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Student);
        var isViewOwn = _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.ViewOwn);
        var isFaculty = _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Faculty);

        if (!isAdmin && !isReadOnly && !isStudent && !isViewOwn && !isFaculty)
        {
            _logger.LogWarning(
                "User {LoginId} attempted to access career selection for PersonId {PersonId} without any authorized role",
                LogSanitizer.SanitizeId(currentUser.LoginId),
                LogSanitizer.SanitizeId(personId.ToString()));
            return ForbidApi();
        }

        if (!isAdmin && !isReadOnly && currentUser.AaudUserId != personId
            && !(isFaculty && await _service.IsMentorOfAsync(currentUser.MothraId, personId)))
        {
            _logger.LogWarning(
                "User {LoginId} attempted to access career selection for PersonId {PersonId}",
                LogSanitizer.SanitizeId(currentUser.LoginId),
                LogSanitizer.SanitizeId(personId.ToString()));
            return ForbidApi();
        }

        var canViewStudentList = isAdmin || isReadOnly || isFaculty;
        var canEdit = _service.CanEdit(personId, currentUser);
        var result = await _service.GetStudentCareerDetailAsync(personId, canEdit, canViewStudentList);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Update or create career selection information for a student.
    /// Admin can update any student. Students can update their own record when permitted.
    /// Returns the updated detail DTO so the frontend can refresh without a second GET.
    /// </summary>
    [HttpPut("{personId:int}")]
    [Permission(Allow = CareerSelectionPermissions.Editors)]
    public async Task<ActionResult<StudentCareerDetailDto>> UpdateStudentCareerSelection(int personId, StudentCareerInfoDto request)
    {
        var currentUser = _userHelper.GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var canEdit = _service.CanEdit(personId, currentUser);
        if (!canEdit)
        {
            _logger.LogWarning(
                "User {LoginId} attempted to update career selection for PersonId {PersonId} without permission",
                LogSanitizer.SanitizeId(currentUser.LoginId),
                LogSanitizer.SanitizeId(personId.ToString()));
            return ForbidApi();
        }

        var isAdmin = _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.Admin);
        var isReadOnly = _userHelper.HasPermission(_rapsContext, currentUser, CareerSelectionPermissions.ReadOnly);

        try
        {
            var errors = await _service.UpdateStudentCareerSelectionAsync(personId, request, isAdmin);
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("CareerSelection", error);
                }
                return ValidationProblem(ModelState);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Cannot update career selection for PersonId {PersonId}: {Message}",
                LogSanitizer.SanitizeId(personId.ToString()),
                LogSanitizer.SanitizeString(ex.Message));
            return NotFound();
        }
        catch (DbUpdateException ex) when (ex.IsDataRejection())
        {
            // Most likely an option that was validated above but deleted before this save landed:
            // the selections hold a foreign key to it and the delete behavior is Restrict.
            _logger.LogWarning(ex,
                "Database error updating career selection for PersonId {PersonId}: {Message}",
                LogSanitizer.SanitizeId(personId.ToString()),
                LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
            return BadRequest(
                "Failed to save the career selection. One of the options may have changed. Reload the page and try again.");
        }

        // Return the refreshed detail so the frontend has updated LastUpdated
        var result = await _service.GetStudentCareerDetailAsync(personId, canEdit, isAdmin || isReadOnly);
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all student career selections formatted for a report.
    /// </summary>
    [HttpGet("report")]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public async Task<ActionResult<List<StudentCareerReportDto>>> GetStudentCareerReport()
    {
        var access = ResolveStudentListAccess();
        if (access.IsDenied)
        {
            return ForbidApi();
        }

        var result = await _service.GetStudentCareerReportAsync(access);
        return Ok(result);
    }

    /// <summary>
    /// Get current access status: whether the app is open for all students,
    /// based on the presence of the .Student permission in the STUDENTS_DVM role.
    /// </summary>
    [HttpGet("access/status")]
    [Permission(Allow = CareerSelectionPermissions.Admin)]
    public async Task<ActionResult<bool>> GetAccessStatus()
    {
        var result = await _service.IsAppOpenAsync();
        return Ok(result);
    }

    /// <summary>
    /// Toggle app-wide student self-service access on or off.
    /// </summary>
    [HttpPost("access/toggle-app")]
    [Permission(Allow = CareerSelectionPermissions.Admin)]
    public async Task<ActionResult<bool>> ToggleAppAccess()
    {
        // A missing RAPS permission or role throws InvalidOperationException, which is a
        // deployment problem rather than anything the admin can act on. It is left to the 500
        // path deliberately, so it is logged at Error with a correlation ID.
        try
        {
            var newState = await _service.ToggleAppAccessAsync();
            return Ok(newState);
        }
        catch (DbUpdateException ex) when (ex.IsDataRejection())
        {
            // Two admins toggling at once: both read no role-permission row and both insert one.
            _logger.LogWarning(ex, "Database error toggling career selection app access: {Message}",
                LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
            return BadRequest(
                "Failed to change student access. Another administrator may have just changed it. Reload the page and try again.");
        }
    }

    /// <summary>
    /// Export the overview (completeness summary) as an Excel file.
    /// </summary>
    [HttpPost("export/overview/excel")]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public Task<ActionResult> ExportOverviewExcel(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CareerSelectionExportRequest? request = null) => ExportAsync(
        request,
        _service.GetStudentCareerListAsync,
        data => ExcelFile(_exportService.GenerateOverviewExcel(data), "CareerSelectionOverview"));

    /// <summary>
    /// Export the overview (completeness summary) as a PDF file. A POST rather than a GET because
    /// the grid's row keys can outgrow a query string.
    /// </summary>
    [HttpPost("export/overview/pdf")]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public Task<ActionResult> ExportOverviewPdf(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CareerSelectionExportRequest? request = null) => ExportAsync(
        request,
        _service.GetStudentCareerListAsync,
        data => InlineFile(_exportService.GenerateOverviewPdf(data), "application/pdf",
            $"CareerSelectionOverview_{DateTime.Now:yyyyMMdd}.pdf"));

    /// <summary>
    /// Export the overview (completeness summary) as a CSV file.
    /// </summary>
    [HttpPost("export/overview/csv")]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public Task<ActionResult> ExportOverviewCsv(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CareerSelectionExportRequest? request = null) => ExportAsync(
        request,
        _service.GetStudentCareerListAsync,
        data => CsvFile(_exportService.GenerateOverviewCsv(data), "CareerSelectionOverview"));

    /// <summary>
    /// Export all career selections as an Excel file.
    /// </summary>
    [HttpPost("export/excel")]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public Task<ActionResult> ExportExcel(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CareerSelectionExportRequest? request = null) => ExportAsync(
        request,
        _service.GetStudentCareerReportAsync,
        data => ExcelFile(_exportService.GenerateExcel(data), "CareerSelection"));

    /// <summary>
    /// Export all career selections as a PDF file. A POST rather than a GET because the grid's
    /// row keys can outgrow a query string.
    /// </summary>
    [HttpPost("export/pdf")]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public Task<ActionResult> ExportPdf(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CareerSelectionExportRequest? request = null) => ExportAsync(
        request,
        _service.GetStudentCareerReportAsync,
        data => InlineFile(_exportService.GeneratePdf(data), "application/pdf",
            $"CareerSelection_{DateTime.Now:yyyyMMdd}.pdf"));

    /// <summary>
    /// Export all career selections as a CSV file.
    /// </summary>
    [HttpPost("export/csv")]
    [Permission(Allow = CareerSelectionPermissions.StudentListViewers)]
    public Task<ActionResult> ExportCsv(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CareerSelectionExportRequest? request = null) => ExportAsync(
        request,
        _service.GetStudentCareerReportAsync,
        data => CsvFile(_exportService.GenerateCsv(data), "CareerSelection"));

    /// <summary>
    /// Runs an export over the students the caller may see: a faculty mentor's export is narrowed
    /// to their mentees, the same as the roster it mirrors, and then to the grid's rows when the
    /// request names them. Nothing to export still produces a file, headers only, as legacy did.
    /// </summary>
    private async Task<ActionResult> ExportAsync<T>(CareerSelectionExportRequest? request,
        Func<StudentListAccess, Task<List<T>>> loadData, Func<List<T>, ActionResult> buildFile)
        where T : StudentCareerRowDto
    {
        var access = ResolveStudentListAccess();
        if (access.IsDenied)
        {
            return ForbidApi();
        }

        var data = await loadData(access);
        return buildFile(CareerSelectionExportRequest.ApplyRowKeys(data, request));
    }
}
