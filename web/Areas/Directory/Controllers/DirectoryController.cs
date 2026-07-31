using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Areas.Directory.Models;
using Viper.Areas.Directory.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Classes.Utilities;
using Viper.Models.AAUD;
using Web.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Viper.Areas.Directory.Controllers
{
    [Area("Directory")]
    [Route("[area]")]
    [Permission(Allow = "SVMSecure")]
    [Authorize(Roles = "VMDO SVM-IT")] //locking directory for now until it's complete
    public class DirectoryController : AreaController
    {
        public AAUDContext _aaud { get; private set; }
        private readonly RAPSContext? _rapsContext;
        public IUserHelper UserHelper { get; private set; }

        public DirectoryController(AAUDContext aaud, RAPSContext rapsContext)
        {
            _aaud = aaud;
            _rapsContext = rapsContext;
            UserHelper = new UserHelper();
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("")]
        public async Task<ActionResult> Index(string? useExample)
        {
            return await Task.Run(() => View("~/Areas/Directory/Views/Card.cshtml", new DirectoryUser()));
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("nav")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav()
        {
            var nav = new List<NavMenuItem>();
            return await Task.Run(() => nav);
        }


        /// <summary>
        /// Directory search via query parameters (handles special characters and avoids race conditions)
        /// </summary>
        [SupportedOSPlatform("windows")]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<IndividualSearchResult>>> GetFromQuery([FromQuery] string search, [FromQuery] bool ucd = false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (string.IsNullOrWhiteSpace(search))
            {
                return Ok(new List<IndividualSearchResult>());
            }
            if (ucd)
            {
                return await GetUCD(search);
            }
            return await Get(search);
        }

        /// <summary>
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        [SupportedOSPlatform("windows")]
        [Route("search/{search}")]
        public async Task<ActionResult<IEnumerable<IndividualSearchResult>>> Get(string search)
        {
            var individuals = await _aaud.AaudUsers
                     .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                         || (u.MailId != null && u.MailId.Contains(search))
                         || (u.LoginId != null && u.LoginId.Contains(search))
                         || (u.SpridenId != null && u.SpridenId.Contains(search))
                         || (u.Pidm != null && u.Pidm.Contains(search))
                         || (u.MothraId != null && u.MothraId.Contains(search))
                         || (u.EmployeeId != null && u.EmployeeId.Contains(search))
                         || (u.IamId != null && u.IamId.Contains(search))
            )
            .Where(u => u.Current != 0)
            .OrderBy(u => u.DisplayLastName)
            .ThenBy(u => u.DisplayFirstName)
                     .ToListAsync();
            List<IndividualSearchResult> results = new();
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            bool hasDetailPermission = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail");
            individuals.ForEach(m =>
            {
                LdapUserContact? l = LdapService.GetUserByID(m.IamId);
                var result = hasDetailPermission
                    ? new IndividualSearchResultWithIDs(m, l)
                    : new IndividualSearchResult(m, l);
                result.LookupEmailHost(_aaud);
                results.Add(result);

                var vmsearch = VMACSService.Search(result.LoginId);
                PopulateVmacsDetails(result, vmsearch.Result);
            });
            return results;
        }

        /// <summary>
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        [SupportedOSPlatform("windows")]
        [Route("search/{search}/ucd")]
        public async Task<ActionResult<IEnumerable<IndividualSearchResult>>> GetUCD(string search)
        {
            List<IndividualSearchResult> results = new();
            List<LdapUserContact> ldap = LdapService.GetUsersContact(search);
            var individuals = await _aaud.AaudUsers
                    .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                        || (u.MailId != null && u.MailId.Contains(search))
                        || (u.LoginId != null && u.LoginId.Contains(search))
                        || (u.SpridenId != null && u.SpridenId.Contains(search))
                        || (u.Pidm != null && u.Pidm.Contains(search))
                        || (u.MothraId != null && u.MothraId.Contains(search))
                        || (u.EmployeeId != null && u.EmployeeId.Contains(search))
                        || (u.IamId != null && u.IamId.Contains(search))
           )
           .Where(u => u.Current != 0)
           .OrderBy(u => u.DisplayLastName)
           .ThenBy(u => u.DisplayFirstName)
                    .ToListAsync();
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            bool hasDetailPermission = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail");
            foreach (var l in ldap)
            {
                AaudUser? userInfo = individuals.Find(m => m.IamId == l.IamId);
                var result = hasDetailPermission
                    ? new IndividualSearchResultWithIDs(userInfo, l)
                    : new IndividualSearchResult(userInfo, l);
                result.LookupEmailHost(_aaud);
                results.Add(result);

                var vmsearch = VMACSService.Search(result.LoginId);
                PopulateVmacsDetails(result, vmsearch.Result);
            }

            return results;
        }

        /// <summary>
        /// Directory results
        /// </summary>
        /// <param name="uid">User ID</param>
        [Route("/[area]/userInfo/{mothraID}")]
        public async Task<IActionResult> DirectoryResult(string mothraID)
        {
            // pull in the user based on uid
            return await Task.Run(() => View("~/Areas/Directory/Views/UserInfo.cshtml"));
        }

        private static void PopulateVmacsDetails(IndividualSearchResult result, VMACSQuery? vm)
        {
            if (vm?.item != null)
            {
                if (vm.item.Nextel != null) result.Nextel = vm.item.Nextel[0];
                if (vm.item.LDPager != null) result.LDPager = vm.item.LDPager[0];
                if (vm.item.Unit != null) result.Department = vm.item.Unit[0];
            }
        }

        [NonAction]
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                                         ActionExecutionDelegate next)
        {
            PopulateLeftNav(context, "viper-home");
            await base.OnActionExecutionAsync(context, next);
        }
    }
}
