using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/svm/frequentnumbers")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneSVMFrequentNumberController(PhoneSVMFrequentNumberService phoneSVMFrequentNumberService) : ApiController
    {
        private readonly PhoneSVMFrequentNumberService _phoneSVMFrequentNumberService = phoneSVMFrequentNumberService;

        /// <summary>
        /// Gets the list of frequently called numbers for the SVM Phone List.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SVMFrequentNumber>>> GetFrequentNumbers(CancellationToken ct = default)
        {
            var results = await _phoneSVMFrequentNumberService.GetSVMFrequentNumbers(ct);
            return Ok(results);
        }

        /// <summary>
        /// Adds a frequently called number to the SVM Phone List.
        /// </summary>
        [HttpPost]
        [Permission(Allow = "SVMSecure.PhoneLists.SVMMaintain")]
        public async Task<ActionResult> AddFrequentNumber(SVMFrequentNumberRequest request, CancellationToken ct = default)
        {
            try
            {
                await _phoneSVMFrequentNumberService.AddFrequentNumber(request, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates a frequently called number in the SVM Phone List.
        /// </summary>
        [HttpPut("{entryId}")]
        [Permission(Allow = "SVMSecure.PhoneLists.SVMMaintain")]
        public async Task<ActionResult> UpdateFrequentNumber(int entryId, SVMFrequentNumberRequest request, CancellationToken ct = default)
        {
            try
            {
                await _phoneSVMFrequentNumberService.UpdateFrequentNumber(entryId, request, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes a frequently called number from the SVM Phone List.
        /// </summary>
        [HttpDelete("{entryId}")]
        [Permission(Allow = "SVMSecure.PhoneLists.SVMMaintain")]
        public async Task<ActionResult> DeleteFrequentNumber(int entryId, CancellationToken ct = default)
        {
            try
            {
                await _phoneSVMFrequentNumberService.DeleteFrequentNumber(entryId, ct);
                return Ok(true);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
