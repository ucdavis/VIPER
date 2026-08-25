using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/svm/modifiedDate")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneSVMModifiedDateController(
        PhoneSVMFrequentNumberService phoneSVMFrequentNumberService,
        PhoneSVMUnitService phoneSVMUnitService) : ApiController
    {
        private readonly PhoneSVMFrequentNumberService _phoneSVMFrequentNumberService = phoneSVMFrequentNumberService;
        private readonly PhoneSVMUnitService _phoneSVMUnitService = phoneSVMUnitService;

        /// <summary>
        /// Identfies when frequent numbers were last modified.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<DateTime?>> GetLastModifiedDate(CancellationToken ct = default)
        {
            var frequentNumberDate = await _phoneSVMFrequentNumberService.GetSVMFrequentNumbersModifiedDate(ct);
            var unitPersonDate = await _phoneSVMUnitService.GetSVMUnitPersonModifiedDate(ct);
            if (frequentNumberDate == null || unitPersonDate != null && unitPersonDate > frequentNumberDate)
            {
                return Ok(unitPersonDate);
            }
            return Ok(frequentNumberDate);
        }
    }
}
