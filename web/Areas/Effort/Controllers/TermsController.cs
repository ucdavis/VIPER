using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Effort.Constants;
using Viper.Areas.Effort.Models.DTOs.Responses;
using Viper.Areas.Effort.Services;
using Web.Authorization;

namespace Viper.Areas.Effort.Controllers;

/// <summary>
/// API controller for term operations in the Effort system.
/// Terms are system-wide data visible to all users with ViewDept permission.
/// </summary>
[Route("/api/effort/terms")]
[Permission(Allow = $"{EffortPermissions.Base},{EffortPermissions.ViewDept},{EffortPermissions.ViewAllDepartments}")]
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
}
