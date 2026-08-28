using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Viper.Classes.Utilities;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/svm/frequentnumbers")]
    [Permission(Allow = "SVMSecure")]
    public class PhoneSVMFrequentNumberController(
        PhoneSVMFrequentNumberService phoneSVMFrequentNumberService,
        ILogger<PhoneSVMFrequentNumberController> logger) : ApiController
    {
        private readonly PhoneSVMFrequentNumberService _phoneSVMFrequentNumberService = phoneSVMFrequentNumberService;
        private readonly ILogger<PhoneSVMFrequentNumberController> _logger = logger;

        /// <summary>
        /// Gets the list of frequently called numbers for the SVM Phone List.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SVMFrequentNumberDto>>> GetFrequentNumbers(CancellationToken ct = default)
        {
            var results = await _phoneSVMFrequentNumberService.GetSVMFrequentNumbers(ct);
            return Ok(PersonnelMapper.ToSVMFrequentNumberDtos(results));
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
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database error adding a frequently called number: {Message}",
                    LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
                return BadRequest("Failed to add the frequently called number. Please check all field values are valid.");
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
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database error updating frequently called number {EntryId}: {Message}",
                    entryId, LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
                return BadRequest("Failed to update the frequently called number. Please check all field values are valid.");
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
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Database error deleting frequently called number {EntryId}: {Message}",
                    entryId, LogSanitizer.SanitizeString(ex.InnerException?.Message ?? ex.Message));
                return BadRequest("Failed to delete the frequently called number. Please try again.");
            }
        }
    }
}
