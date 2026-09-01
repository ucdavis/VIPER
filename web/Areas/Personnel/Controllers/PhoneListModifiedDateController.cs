using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/phonelist/{code}/modifiedDate")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneListModifiedDateController(
        PhoneListService phoneListService,
        PhoneListUnitService phoneListUnitService) : ApiController
    {
        private readonly PhoneListService _phoneListService = phoneListService;
        private readonly PhoneListUnitService _phoneListUnitService = phoneListUnitService;

        /// <summary>
        /// Returns the latest modification date of a UnitPerson in this list.
        /// Includes deleted rows.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<DateTime?>> GetLastModifiedDate(string code, CancellationToken ct = default)
        {
            try
            {
                var list = await _phoneListService.GetListByCode(code, ct);
                var unitPersonDate = await _phoneListUnitService.GetUnitPersonModifiedDate(list.PhoneListId, ct);
                return Ok(unitPersonDate);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
