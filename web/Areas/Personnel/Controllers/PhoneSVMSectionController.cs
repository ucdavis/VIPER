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
        /// Gets the sections to include in the SVM Phone List.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SVMSection>>> GetSections(CancellationToken ct = default)
        {
            var results = await _phoneSVMSectionService.GetSVMSections(ct);
            if (results.Count == 0)
            {
                return NotFound("No sections for the SVM Phone List were found.");
            }
            return Ok(results);
        }
    }
}
