using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.ClinicalScheduler.Models.DTOs.Responses;
using Viper.Areas.ClinicalScheduler.Services;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.ClinicalScheduler.Controllers
{
    /// <summary>
    /// Read-only access to the schedule-change audit log. Replaces the legacy
    /// "Schedule Changes Audit log" page and is gated by the same Manage permission.
    /// </summary>
    [Route("api/clinicalscheduler/audit")]
    [ApiController]
    [Permission(Allow = ClinicalSchedulePermissions.Manage)]
    public class ScheduleAuditController : BaseClinicalSchedulerController
    {
        private readonly IScheduleAuditService _auditService;

        public ScheduleAuditController(
            IScheduleAuditService auditService,
            IGradYearService gradYearService,
            ILogger<ScheduleAuditController> logger)
            : base(gradYearService, logger)
        {
            _auditService = auditService;
        }

        /// <summary>
        /// Get the filtered audit log for a grad year (defaults to the current grad year).
        /// </summary>
        /// <param name="year">Grad year; defaults to the current grad year when omitted</param>
        /// <param name="rotationId">Optional rotation filter</param>
        /// <param name="person">Optional MothraID of the affected student/clinician</param>
        /// <param name="modifiedBy">Optional MothraID of the user who made the change</param>
        /// <param name="area">Optional area filter (Students / Clinicians)</param>
        /// <param name="from">Optional inclusive lower bound on the change date</param>
        /// <param name="to">Optional inclusive upper bound on the change date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet]
        [ProducesResponseType(typeof(List<AuditLogEntryDto>), 200)]
        public async Task<IActionResult> GetAuditLog(
            [FromQuery] int? year = null,
            [FromQuery] int? rotationId = null,
            [FromQuery] string? person = null,
            [FromQuery] string? modifiedBy = null,
            [FromQuery] string? area = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var gradYear = await GetTargetYearAsync(year);
                var log = await _auditService.GetAuditLogAsync(
                    gradYear, rotationId, person, modifiedBy, area, from, to, cancellationToken);
                return Ok(log);
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error retrieving audit log for year {Year}", LogSanitizer.SanitizeYear(year));
                return StatusCode(500, "An error occurred while retrieving the audit log");
            }
        }

        /// <summary>
        /// Get the distinct users who have made audited schedule changes, for the "Modified By" filter.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("modifiers")]
        [ProducesResponseType(typeof(List<AuditModifierDto>), 200)]
        public async Task<IActionResult> GetModifiers(CancellationToken cancellationToken = default)
        {
            try
            {
                var modifiers = await _auditService.GetAuditModifiersAsync(cancellationToken);
                return Ok(modifiers);
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error retrieving audit log modifiers");
                return StatusCode(500, "An error occurred while retrieving the audit modifiers");
            }
        }

        /// <summary>
        /// Get the distinct affected students/clinicians in the audit trail, for the "Person" filter.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("persons")]
        [ProducesResponseType(typeof(List<AuditModifierDto>), 200)]
        public async Task<IActionResult> GetPersons(CancellationToken cancellationToken = default)
        {
            try
            {
                var persons = await _auditService.GetAuditPersonsAsync(cancellationToken);
                return Ok(persons);
            }
            catch (Exception ex) when (ex is DbUpdateException or SqlException or InvalidOperationException)
            {
                _logger.LogError(ex, "Error retrieving audit log persons");
                return StatusCode(500, "An error occurred while retrieving the audited persons");
            }
        }
    }
}
