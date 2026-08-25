using Microsoft.AspNetCore.Mvc;
using Viper.Areas.Personnel.Models;
using Viper.Areas.Personnel.Services;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Personnel.Controllers
{
    [Route("/api/phones/people")]
    [Permission(Allow = "SVMSecure")]
    public class PhonePersonController(
        PhoneListService phoneListService,
        PhonePersonLookupService phonePersonService) : ApiController
    {
        private readonly PhoneListService _phoneListService = phoneListService;
        private readonly PhonePersonLookupService _phonePersonService = phonePersonService;

        /// <summary>
        /// Person picker for the phone-record dialogs. Only returns direct numbers if the user
        /// can edit the current list (and so has access to the data).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<AugmentedViperPerson>>> GetCurrentEmployees(string search, string? listCode = null, CancellationToken ct = default)
        {
            PhoneList? list = null;
            if (!string.IsNullOrWhiteSpace(listCode))
            {
                try
                {
                    list = await _phoneListService.GetListByCode(listCode, ct);
                }
                catch (InvalidOperationException)
                {
                    list = null;
                }
            }

            List<ViperPerson> viperResults = await _phonePersonService.GetViperCurrentEmployees(search, ct);
            List<string> iamIds = [];
            foreach (ViperPerson result in viperResults)
            {
                iamIds.Add(result.IamId);
            }
            List<PhonePerson> phoneResults = await _phonePersonService.GetPhonePeople(iamIds, list, ct);
            Dictionary<string, AugmentedViperPerson> mergedResultsDict = [];
            foreach (ViperPerson result in viperResults)
            {
                mergedResultsDict[result.IamId] = PersonnelMapper.ToAugmentedViperPerson(result);
            }
            List<PhonePerson> matchingResults = [.. phoneResults.Where(x => mergedResultsDict.ContainsKey(x.PersonIam))];

            foreach (PhonePerson result in matchingResults)
            {
                mergedResultsDict[result.PersonIam].AddPhoneData(result);
            }
            return Ok(mergedResultsDict.Values.ToList());
        }
    }
}
