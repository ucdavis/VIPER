using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models.Eis;
using Viper.Areas.Personnel.Services;
using Viper.Areas.Students.Services;
using Viper.Classes;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers;

/// <summary>
/// The Employee Information System (EIS). Any EIS user reaches the controller; each person
/// request then checks that this user may view that employee before reading anything, so a
/// department user can't open someone outside their units by editing the URL.
/// </summary>
[Route("/api/personnel/eis")]
[Permission(Allow = EisPermissions.View)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EisController : ApiController
{
    private const string NotAvailable = "This employee is not available in EIS.";

    private readonly IEisService _eis;
    private readonly IPhotoService _photos;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<EisController> _logger;

    public EisController(IEisService eis, IPhotoService photos, IUserHelper userHelper, ILogger<EisController> logger)
    {
        _eis = eis;
        _photos = photos;
        _userHelper = userHelper;
        _logger = logger;
    }

    /// <summary>The people this user may look up, for the person picker.</summary>
    [HttpGet("people")]
    public async Task<ActionResult<IReadOnlyList<EisPersonOption>>> GetPeople(CancellationToken ct = default)
    {
        return Ok(await _eis.GetPeopleAsync(ct));
    }

    [HttpGet("people/{employeeId}")]
    public async Task<ActionResult<EisPersonHeader>> GetHeader(string employeeId, CancellationToken ct = default)
    {
        if (await RefuseAsync(employeeId, "header", ct) is { } refused)
        {
            return refused;
        }

        EisPersonHeader? header = await _eis.GetHeaderAsync(employeeId, ct);
        if (header is null)
        {
            return NotFound(NotAvailable);
        }

        return Ok(header);
    }

    [HttpGet("people/{employeeId}/appointments")]
    public async Task<ActionResult<EisAppointments>> GetAppointments(string employeeId, CancellationToken ct = default)
    {
        return await RefuseAsync(employeeId, "appointments", ct) ?? Ok(await _eis.GetAppointmentsAsync(employeeId, ct));
    }

    [HttpGet("people/{employeeId}/history")]
    public async Task<ActionResult<EisHistory>> GetHistory(string employeeId, CancellationToken ct = default)
    {
        return await RefuseAsync(employeeId, "history", ct) ?? Ok(await _eis.GetHistoryAsync(employeeId, ct));
    }

    [HttpGet("people/{employeeId}/address")]
    public async Task<ActionResult<EisAddress>> GetAddress(string employeeId, CancellationToken ct = default)
    {
        return await RefuseAsync(employeeId, "address", ct) ?? Ok(await _eis.GetAddressAsync(employeeId, ct));
    }

    [HttpGet("people/{employeeId}/academics")]
    public async Task<ActionResult<EisAcademics>> GetAcademics(string employeeId, CancellationToken ct = default)
    {
        return await RefuseAsync(employeeId, "academics", ct) ?? Ok(await _eis.GetAcademicsAsync(employeeId, ct));
    }

    [HttpGet("people/{employeeId}/categories")]
    public async Task<ActionResult<EisAppointmentCategories>> GetCategories(string employeeId, CancellationToken ct = default)
    {
        return await RefuseAsync(employeeId, "categories", ct) ?? Ok(await _eis.GetAppointmentCategoriesAsync(employeeId, ct));
    }

    /// <summary>Sets a manual appointment category for the current academic year.</summary>
    [HttpPut("people/{employeeId}/flags/{code:int}")]
    [Permission(Allow = EisPermissions.Admin)]
    public Task<ActionResult<EisAppointmentCategories>> SetFlag(string employeeId, int code, CancellationToken ct = default)
    {
        return ChangeFlagAsync(employeeId, code, true, ct);
    }

    /// <summary>Clears a manual appointment category for the current academic year.</summary>
    [HttpDelete("people/{employeeId}/flags/{code:int}")]
    [Permission(Allow = EisPermissions.Admin)]
    public Task<ActionResult<EisAppointmentCategories>> ClearFlag(string employeeId, int code, CancellationToken ct = default)
    {
        return ChangeFlagAsync(employeeId, code, false, ct);
    }

    /// <summary>The employee's ID card photo, or the default photo when there is none.</summary>
    [HttpGet("people/{employeeId}/photo")]
    public async Task<ActionResult> GetPhoto(string employeeId, CancellationToken ct = default)
    {
        if (!EisService.IsValidEmployeeId(employeeId) || !await _eis.CanViewAsync(employeeId, ct))
        {
            return NotFound();
        }

        string? mailId = await _eis.GetMailIdAsync(employeeId, ct);
        byte[] photo = await _photos.GetStudentPhotoAsync(mailId ?? string.Empty);
        return File(photo, "image/jpeg");
    }

    private async Task<ActionResult<EisAppointmentCategories>> ChangeFlagAsync(
        string employeeId, int code, bool isSet, CancellationToken ct)
    {
        if (await RefuseAsync(employeeId, "categories", ct) is { } refused)
        {
            return refused;
        }

        if (!EisService.IsFlagCode(code))
        {
            return NotFound("Unknown appointment category.");
        }

        EisAppointmentCategories? categories = await _eis.SetFlagAsync(employeeId, code, isSet, ct);
        if (categories is null)
        {
            return ForbidApi("You do not have permission to change appointment categories.");
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "EIS category {Code} {Action} for employee {EmployeeId} by {LoginId}",
                code,
                isSet ? "set" : "cleared",
                LogSanitizer.SanitizeId(employeeId),
                LogSanitizer.SanitizeId(_userHelper.GetCurrentUser()?.LoginId));
        }

        return Ok(categories);
    }

    /// <summary>
    /// Null when this user may view the employee, after logging the access: EIS shows dates of
    /// birth, salaries and home addresses. Otherwise the response that refuses the request.
    /// </summary>
    private async Task<ActionResult?> RefuseAsync(string employeeId, string page, CancellationToken ct)
    {
        if (!EisService.IsValidEmployeeId(employeeId))
        {
            return NotFound(NotAvailable);
        }

        if (!await _eis.CanViewAsync(employeeId, ct))
        {
            return ForbidApi("You do not have access to this employee.");
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "EIS {Page} for employee {EmployeeId} viewed by {LoginId}",
                page,
                LogSanitizer.SanitizeId(employeeId),
                LogSanitizer.SanitizeId(_userHelper.GetCurrentUser()?.LoginId));
        }

        return null;
    }
}
