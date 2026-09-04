using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/svm/sections")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneSVMSectionController(PhoneSVMSectionService phoneSVMSectionService) : ApiController
    {
        private readonly PhoneSVMSectionService _phoneSVMSectionService = phoneSVMSectionService;

        /// <summary>
        /// Gets the sections to include in the SVM Phone List. A list with no sections yet is an
        /// empty result rather than a 404: the client renders no section tables, where an error
        /// status would raise the global error banner over a page that is simply new.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SVMSectionDto>>> GetSections(CancellationToken ct = default)
        {
            var results = await _phoneSVMSectionService.GetSVMSections(ct);
            return Ok(PersonnelMapper.ToSVMSectionDtos(results));
        }
    }
}
