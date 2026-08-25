using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/svm")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneSVMUnitController(PhoneSVMUnitService phoneSVMUnitService) : ApiController
    {
        private readonly PhoneSVMUnitService _phoneSVMUnitService = phoneSVMUnitService;

        /// <summary>
        /// Gets all units for every section in the SVM Phone List.
        /// </summary>
        [HttpGet("units")]
        public async Task<ActionResult<List<SVMUnit>>> GetUnits(CancellationToken ct = default)
        {
            var results = await _phoneSVMUnitService.GetSVMUnits(ct);
            return Ok(results);
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
        }
    }
}
