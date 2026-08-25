using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    /// <summary>
    /// Unit and unit-person endpoints for a phone list, addressed by the list's stable Code.
    /// Write access is the role named by that list's MaintainRole column, so each list
    /// can have separate permissions.
    /// </summary>
    [Route("/api/phones/phonelist/{code}")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneListUnitController(
        PhoneListService phoneListService,
        PhoneListUnitService phoneListUnitService,
        PhonesPermissionsService phonesPermissionsService) : ApiController
    {
        private readonly PhoneListService _phoneListService = phoneListService;
        private readonly PhoneListUnitService _phoneListUnitService = phoneListUnitService;
        private readonly PhonesPermissionsService _phonesPermissionsService = phonesPermissionsService;

        /// <summary>
        /// Resolves the list named in the route and confirms the caller may edit it. Returns the
        /// list id on success, or the ActionResult to return to the caller on failure.
        /// </summary>
        private async Task<(int ListId, ActionResult? Failure)> ResolveListForMaintain(string code, CancellationToken ct)
        {
            PhoneList list;
            try
            {
                list = await _phoneListService.GetListByCode(code, ct);
            }
            catch (InvalidOperationException ex)
            {
                return (0, NotFound(ex.Message));
            }
            if (!_phonesPermissionsService.CanMaintainList(list))
            {
                return (0, Forbid());
            }
            return (list.PhoneListId, null);
        }

        /// <summary>
        /// Retrieves the PhoneListUnits associated with a given list code, including the PhoneListUnitPersons
        /// in that unit.
        /// </summary>
        [HttpGet("units")]
        public async Task<ActionResult<List<PhoneListUnit>>> GetUnits(string code, CancellationToken ct = default)
        {
            try
            {
                var list = await _phoneListService.GetListByCode(code, ct);
                var results = await _phoneListUnitService.GetPhoneListUnits(list, ct);
                return Ok(results);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Adds a unit person to a given list, provided the user has appropriate permissions.
        /// </summary>
        [HttpPost("unitPerson")]
        public async Task<ActionResult> AddUnitPersonData(string code, PhoneListUnitDataRequest request, CancellationToken ct = default)
        {
            var (listId, failure) = await ResolveListForMaintain(code, ct);
            if (failure != null)
            {
                return failure;
            }
            try
            {
                await _phoneListUnitService.AddUnitPersonData(listId, request, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates a unit person in a given list, provided the user has appropriate permissions.
        /// </summary>
        [HttpPut("unitPerson/{unitPersonId}")]
        public async Task<ActionResult> UpdateUnitPersonData(string code, int unitPersonId, PhoneListUnitDataRequest request, CancellationToken ct = default)
        {
            var (listId, failure) = await ResolveListForMaintain(code, ct);
            if (failure != null)
            {
                return failure;
            }
            try
            {
                await _phoneListUnitService.UpdateUnitPersonData(listId, unitPersonId, request, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a unit person from a given list, provided the user has appropriate permissions.
        /// </summary>
        [HttpDelete("unitPerson/{unitPersonId}")]
        public async Task<ActionResult> DeleteUnitPersonData(string code, int unitPersonId, CancellationToken ct = default)
        {
            var (listId, failure) = await ResolveListForMaintain(code, ct);
            if (failure != null)
            {
                return failure;
            }
            try
            {
                await _phoneListUnitService.DeleteUnitPersonData(listId, unitPersonId, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
