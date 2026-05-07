using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Scheduler.Models.DTOs.Responses;
using Viper.Areas.Scheduler.Services;
using Viper.Classes;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Scheduler.Controllers;

/// <summary>
/// Operator API for the recurring-job scheduler. List, pause, resume.
/// All endpoints require <c>SVMSecure.CATS.scheduledJobs</c> (the same
/// permission the legacy ColdFusion VIPER scheduler checks). System jobs
/// in the reserved <c>__scheduler:</c> namespace are read-only via this
/// surface; the dashboard remains the break-glass console for them.
/// </summary>
[Route("/api/scheduler/jobs")]
[Permission(Allow = "SVMSecure.CATS.scheduledJobs")]
public class JobsController : ApiController
{
    private const string SystemJobErrorCode = "system_job_not_pausable";
    private const string ConcurrencyErrorCode = "concurrency_conflict";
    private const string MissingMarkerErrorCode = "marker_not_found";
    private const string InvalidRowVersionErrorCode = "invalid_row_version";

    private readonly ISchedulerJobsService _schedulerJobsService;
    private readonly IUserHelper _userHelper;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        ISchedulerJobsService schedulerJobsService,
        IUserHelper userHelper,
        ILogger<JobsController> logger)
    {
        _schedulerJobsService = schedulerJobsService;
        _userHelper = userHelper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<SchedulerJobDto>>> ListJobs(CancellationToken ct)
    {
        var jobs = await _schedulerJobsService.ListJobsAsync(ct);
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SchedulerJobDto>> GetJob(string id, CancellationToken ct)
    {
        var job = await _schedulerJobsService.GetJobAsync(id, ct);
        if (job == null)
        {
            return NotFound();
        }
        return Ok(job);
    }

    [HttpPost("{id}/pause")]
    public async Task<ActionResult<PauseResumeResultDto>> PauseJob(
        string id,
        [FromBody] PauseRequest? request,
        CancellationToken ct)
    {
        var modBy = ResolveModBy();
        var expectedRowVersion = TryDecodeRowVersion(request?.RowVersion);
        if (request?.RowVersion != null && expectedRowVersion == null)
        {
            return BadRequest(new { error = InvalidRowVersionErrorCode });
        }

        try
        {
            var result = await _schedulerJobsService.PauseJobAsync(id, modBy, expectedRowVersion, ct);
            if (result.DeregistrationPending)
            {
                return StatusCode(StatusCodes.Status202Accepted, result);
            }
            return Ok(result);
        }
        catch (SchedulerSystemJobProtectedException ex)
        {
            _logger.LogWarning(ex, "Pause refused for system job {JobId}", LogSanitizer.SanitizeId(id));
            return StatusCode(StatusCodes.Status403Forbidden, new { error = SystemJobErrorCode });
        }
        catch (SchedulerJobNotFoundException)
        {
            return NotFound();
        }
        catch (SchedulerConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Pause concurrency conflict for {JobId}", LogSanitizer.SanitizeId(id));
            return Conflict(new { error = ConcurrencyErrorCode });
        }
    }

    [HttpPost("{id}/resume")]
    public async Task<ActionResult<PauseResumeResultDto>> ResumeJob(
        string id,
        [FromBody] ResumeRequest? request,
        CancellationToken ct)
    {
        var expectedRowVersion = TryDecodeRowVersion(request?.RowVersion);
        if (expectedRowVersion == null)
        {
            return BadRequest(new { error = InvalidRowVersionErrorCode });
        }

        try
        {
            var result = await _schedulerJobsService.ResumeJobAsync(id, expectedRowVersion, ct);
            return Ok(result);
        }
        catch (SchedulerSystemJobProtectedException ex)
        {
            _logger.LogWarning(ex, "Resume refused for system job {JobId}", LogSanitizer.SanitizeId(id));
            return StatusCode(StatusCodes.Status403Forbidden, new { error = SystemJobErrorCode });
        }
        catch (SchedulerJobNotFoundException)
        {
            return NotFound(new { error = MissingMarkerErrorCode });
        }
        catch (SchedulerConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Resume concurrency conflict for {JobId}", LogSanitizer.SanitizeId(id));
            return Conflict(new { error = ConcurrencyErrorCode });
        }
    }

    private string ResolveModBy()
    {
        var current = _userHelper.GetCurrentUser();
        return current?.LoginId ?? ISchedulerJobsService.SchedulerActor;
    }

    private static byte[]? TryDecodeRowVersion(string? base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return null;
        }
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public class PauseRequest
    {
        /// <summary>Base64 rowversion from the matching ListJobs entry; optional on first pause.</summary>
        public string? RowVersion { get; set; }
    }

    public class ResumeRequest
    {
        /// <summary>Base64 rowversion from the matching ListJobs entry; required.</summary>
        public string? RowVersion { get; set; }
    }
}
