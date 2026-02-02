using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Requests;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for term operations in the Effort system.
/// Basic term lookup accessible to users with VerifyEffort permission (self-service).
/// Management endpoints require ManageTerms permission.
/// </summary>
[Route("/api/effort/terms")]
[Permission(Allow = $"{EffortPermissions.Base},{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments},{EffortPermissions.VerifyEffort}")]
public class TermsController : BaseEffortController
{
    private readonly ITermService _termService;

    public TermsController(
        ITermService termService,
        ILogger<TermsController> logger) : base(logger)
    {
        _termService = termService;
    }

    /// <summary>
    /// Get all terms with their effort workflow status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TermDto>>> GetTerms(CancellationToken ct)
    {
        var terms = await _termService.GetTermsAsync(ct);
        return Ok(terms);
    }

    /// <summary>
    /// Get a specific term by term code.
    /// </summary>
    [HttpGet("{termCode:int}")]
    public async Task<ActionResult<TermDto>> GetTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var term = await _termService.GetTermAsync(termCode, ct);

        if (term == null)
        {
            _logger.LogWarning("Term not found: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        return Ok(term);
    }

    /// <summary>
    /// Get the current open term (most recent with status "Opened").
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<TermDto>> GetCurrentTerm(CancellationToken ct)
    {
        var term = await _termService.GetCurrentTermAsync(ct);

        if (term == null)
        {
            _logger.LogInformation("No open term found");
            return NotFound("No open term found");
        }

        return Ok(term);
    }

    // Term Management Endpoints (require ManageTerms permission)

    /// <summary>
    /// Create a new term.
    /// </summary>
    [HttpPost]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<TermDto>> CreateTerm([FromBody] CreateTermRequest request, CancellationToken ct)
    {
        SetExceptionContext("termCode", request.TermCode);

        try
        {
            var term = await _termService.CreateTermAsync(request.TermCode, ct);
            _logger.LogInformation("Term created: {TermCode}", request.TermCode);
            return CreatedAtAction(nameof(GetTerm), new { termCode = term.TermCode }, term);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot create term {TermCode}: {Message}", request.TermCode, LogSanitizer.SanitizeString(ex.Message));
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Delete a term. Only succeeds if no courses, persons, or records exist.
    /// </summary>
    [HttpDelete("{termCode:int}")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult> DeleteTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var deleted = await _termService.DeleteTermAsync(termCode, ct);

        if (!deleted)
        {
            _logger.LogWarning("Cannot delete term {TermCode}: term has related data or not found", termCode);
            return BadRequest($"Cannot delete term {termCode}: term has related courses, persons, or records, or was not found");
        }

        _logger.LogInformation("Term deleted: {TermCode}", termCode);
        return NoContent();
    }

    /// <summary>
    /// Open a term for effort entry.
    /// </summary>
    [HttpPost("{termCode:int}/open")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<TermDto>> OpenTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        TermDto? term;
        try
        {
            term = await _termService.OpenTermAsync(termCode, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot open term {TermCode}: {Message}", termCode, LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }

        if (term == null)
        {
            _logger.LogWarning("Term not found for open: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        _logger.LogInformation("Term opened: {TermCode}", termCode);
        return Ok(term);
    }

    /// <summary>
    /// Close a term. Validates no courses have zero enrollment.
    /// </summary>
    [HttpPost("{termCode:int}/close")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<TermDto>> CloseTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        var (success, errorMessage) = await _termService.CloseTermAsync(termCode, ct);

        if (!success)
        {
            _logger.LogWarning("Cannot close term {TermCode}: {ErrorMessage}", termCode, LogSanitizer.SanitizeString(errorMessage));
            return BadRequest(errorMessage);
        }

        var term = await _termService.GetTermAsync(termCode, ct);
        _logger.LogInformation("Term closed: {TermCode}", termCode);
        return Ok(term);
    }

    /// <summary>
    /// Reopen a closed term.
    /// </summary>
    [HttpPost("{termCode:int}/reopen")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<TermDto>> ReopenTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        TermDto? term;
        try
        {
            term = await _termService.ReopenTermAsync(termCode, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot reopen term {TermCode}: {Message}", termCode, LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }

        if (term == null)
        {
            _logger.LogWarning("Term not found for reopen: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        _logger.LogInformation("Term reopened: {TermCode}", termCode);
        return Ok(term);
    }

    /// <summary>
    /// Revert an open term back to harvested/created state.
    /// </summary>
    [HttpPost("{termCode:int}/unopen")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<TermDto>> UnopenTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);

        TermDto? term;
        try
        {
            term = await _termService.UnopenTermAsync(termCode, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot unopen term {TermCode}: {Message}", termCode, LogSanitizer.SanitizeString(ex.Message));
            return BadRequest(ex.Message);
        }

        if (term == null)
        {
            _logger.LogWarning("Term not found for unopen: {TermCode}", termCode);
            return NotFound($"Term {termCode} not found");
        }

        _logger.LogInformation("Term unopened: {TermCode}", termCode);
        return Ok(term);
    }

    /// <summary>
    /// Check if a term can be deleted.
    /// </summary>
    [HttpGet("{termCode:int}/can-delete")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<bool>> CanDeleteTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);
        var canDelete = await _termService.CanDeleteTermAsync(termCode, ct);
        return Ok(canDelete);
    }

    /// <summary>
    /// Check if a term can be closed and get zero enrollment count.
    /// </summary>
    [HttpGet("{termCode:int}/can-close")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<object>> CanCloseTerm(int termCode, CancellationToken ct)
    {
        SetExceptionContext("termCode", termCode);
        var (canClose, zeroEnrollmentCount) = await _termService.CanCloseTermAsync(termCode, ct);
        return Ok(new { canClose, zeroEnrollmentCount });
    }

    /// <summary>
    /// Get future terms from vwTerms that are not yet in the Effort system.
    /// </summary>
    [HttpGet("available")]
    [Permission(Allow = EffortPermissions.ManageTerms)]
    public async Task<ActionResult<IEnumerable<AvailableTermDto>>> GetAvailableTerms(CancellationToken ct)
    {
        var terms = await _termService.GetAvailableTermsAsync(ct);
        return Ok(terms);
    }
}
