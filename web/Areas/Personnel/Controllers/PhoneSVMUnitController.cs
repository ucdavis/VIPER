using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/svm")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneSVMUnitController(
        PhoneSVMUnitService phoneSVMUnitService,
        ILogger<PhoneSVMUnitController> logger) : ApiController
    {
        private readonly PhoneSVMUnitService _phoneSVMUnitService = phoneSVMUnitService;
        private readonly ILogger<PhoneSVMUnitController> _logger = logger;

        /// <summary>
        /// Gets all units for every section in the SVM Phone List.
        /// </summary>
        [HttpGet("units")]
        public async Task<ActionResult<List<SVMUnitDto>>> GetUnits(CancellationToken ct = default)
        {
            var results = await _phoneSVMUnitService.GetSVMUnits(ct);
            return Ok(PersonnelMapper.ToSVMUnitDtos(results));
        }

        /// <summary>
        /// Adds data to a unit for the SVM Phone List.
        /// This affects both SVMUnit and SVMUnitPerson.
        /// </summary>
        [HttpPost("units/{unitId}")]
        [Permission(Allow = "SVMSecure.PhoneLists.SVMMaintain")]
        public async Task<ActionResult> AddUnitData(int unitId, SVMUnitDataRequest request, CancellationToken ct = default)
        {
            try
            {
                await _phoneSVMUnitService.AddOrUpdateUnitData(unitId, request, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex) when (ex.IsDataRejection())
            {
                _logger.LogWarning(ex, "Database error adding data to SVM unit {UnitId}: {Message}",
                    unitId, LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
                return BadRequest("Failed to add the record. Please check all field values are valid.");
            }
        }

        /// <summary>
        /// Updates data in a unit for the SVM Phone List.
        /// This affects both SVMUnit and SVMUnitPerson.
        /// </summary>
        [HttpPut("units/{unitId}")]
        [Permission(Allow = "SVMSecure.PhoneLists.SVMMaintain")]
        public async Task<ActionResult> UpdateUnitData(int unitId, SVMUnitDataRequest request, CancellationToken ct = default)
        {
            try
            {
                await _phoneSVMUnitService.AddOrUpdateUnitData(unitId, request, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex) when (ex.IsDataRejection())
            {
                _logger.LogWarning(ex, "Database error updating data in SVM unit {UnitId}: {Message}",
                    unitId, LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
                return BadRequest("Failed to update the record. Please check all field values are valid.");
            }
        }

        /// <summary>
        /// Deletes one row of the SVM list, identified by the row key the list renders.
        /// This may delete multiple SVMUnitPerson.
        /// Handled this way to match the end user experience and wrap multiple
        /// deletions in a transaction.
        /// </summary>
        [HttpDelete("rows/{entryId}")]
        [Permission(Allow = "SVMSecure.PhoneLists.SVMMaintain")]
        public async Task<ActionResult> DeleteUnitRow(int entryId, CancellationToken ct = default)
        {
            try
            {
                await _phoneSVMUnitService.DeleteUnitRow(entryId, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateException ex) when (ex.IsDataRejection())
            {
                _logger.LogWarning(ex, "Database error deleting SVM list row {EntryId}: {Message}",
                    entryId, LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
                return BadRequest("Failed to delete the record. Please try again.");
            }
        }
    }
}
