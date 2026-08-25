using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/phonelist")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneListController(
        PhoneListService phoneListService,
        PhoneListUnitService phoneListUnitService,
        PhonesPermissionsService phonesPermissionsService) : ApiController
    {
        private readonly PhoneListService _phoneListService = phoneListService;
        private readonly PhoneListUnitService _phoneListUnitService = phoneListUnitService;
        private readonly PhonesPermissionsService _phonesPermissionsService = phonesPermissionsService;

        /// <summary>
        /// Everything a client needs before it fetches rows:
        /// Returns the list's display name plus this caller's permissions.
        /// Returned together to reduce API calls on the front end.
        /// The backend continues to enforce permissions, but the front end
        /// knows what data to expect and display based on these results.
        /// </summary>
        [HttpGet("{code}")]
        public async Task<ActionResult<PhoneListInfo>> GetListInfo(string code, CancellationToken ct = default)
        {
            try
            {
                var list = await _phoneListService.GetListByCode(code, ct);
                return Ok(new PhoneListInfo
                {
                    PhoneListId = list.PhoneListId,
                    Code = list.Code,
                    Name = list.Name,
                    CanMaintain = _phonesPermissionsService.CanMaintainList(list),
                    CanViewDirectPhone = await _phoneListUnitService.CanViewDirectPhone(list, ct),
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
