using System.Runtime.Versioning;
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
    [Route("/[area]")]
    [Permission(Allow = "SVMSecure")]
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
        public ActionResult Index(string? useExample)
        {
            return View("~/Areas/Directory/Views/Card.cshtml", new DirectoryUser());
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("nav")]
        public ActionResult<IEnumerable<NavMenuItem>> Nav()
        {
            var nav = new List<NavMenuItem>();
            return nav;
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
            var individuals = await SearchCurrentOrFutureAaudUsers(_aaud, search);
            List<IndividualSearchResult> results = new();
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            bool hasDetailPermission = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail");
            foreach (var m in individuals)
            {
                LdapUserContact? l = LdapService.GetUserByID(m.IamId);
                var result = hasDetailPermission
                    ? new IndividualSearchResultWithIDs(m, l)
                    : new IndividualSearchResult(m, l);
                result.LookupEmailHost(_aaud);
                results.Add(result);
                await AddVmacsContactInfoAsync(result);
            }
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
            var individuals = await SearchCurrentOrFutureAaudUsers(_aaud, search);
            var individualsByIamId = individuals.ToLookup(m => m.IamId);
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            bool hasDetailPermission = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail");
            foreach (var l in ldap)
            {
                AaudUser? userInfo = individualsByIamId[l.IamId].FirstOrDefault();
                var result = hasDetailPermission
                    ? new IndividualSearchResultWithIDs(userInfo, l)
                    : new IndividualSearchResult(userInfo, l);
                result.LookupEmailHost(_aaud);
                results.Add(result);
                await AddVmacsContactInfoAsync(result);
            }
            return results;
        }

        private static void PopulateVmacsDetails(IndividualSearchResult result, VMACSQuery? vm)
        {
            if (vm?.item != null)
            {
                if (vm.item.Nextel?.Length > 0) result.Nextel = vm.item.Nextel[0];
                if (vm.item.LDPager?.Length > 0) result.LDPager = vm.item.LDPager[0];
                if (vm.item.Unit?.Length > 0) result.Department = vm.item.Unit[0];
            }
        }

        [NonAction]
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                                         ActionExecutionDelegate next)
        {
            PopulateLeftNav(context, "viper-home");
            await base.OnActionExecutionAsync(context, next);
        }
        /// <summary>
        /// Current or future AAUD users matching the search term on name or any directory identifier,
        /// ordered for display. Shared by Get and GetUCD.
        /// </summary>
        /// <remarks>
        /// The identifiers are checked through an inline collection rather than an OR chain on purpose. The
        /// chain trips cs/complex-condition, and its MothraId null check is dead code since MothraId is the
        /// one non-nullable identifier on AaudUser.
        ///
        /// Current alone excludes people who are admitted/registered for an upcoming term that hasn't
        /// started yet (e.g. an incoming student before the quarter begins) - AAUD only flips
        /// current_student/current_employee on once the term is actually underway. Future covers that gap,
        /// matching the population the campus LDAP directory and the legacy ColdFusion directory show.
        /// </remarks>
        internal static Task<List<AaudUser>> SearchCurrentOrFutureAaudUsers(AAUDContext aaud, string search)
        {
            return aaud.AaudUsers
                .AsNoTracking()
                .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                    || new[] { u.MailId, u.LoginId, u.SpridenId, u.Pidm, u.MothraId, u.EmployeeId, u.IamId }
                        .Any(id => id != null && id.Contains(search)))
                .Where(u => u.Current != 0 || u.Future != 0)
                .OrderBy(u => u.DisplayLastName)
                .ThenBy(u => u.DisplayFirstName)
                .ToListAsync();
        }

        /// <summary>
        /// Add VMACS phone/pager/department info to a search result when the lookup finds a match.
        /// </summary>
        private static async Task AddVmacsContactInfoAsync(IndividualSearchResult result)
        {
            // Without a login ID the VMACS query would run with an empty find value;
            // skip the pointless lookup. Empty element lists deserialize as empty
            // arrays (not null), so guard on length before indexing.
            if (string.IsNullOrWhiteSpace(result.LoginId))
            {
                return;
            }
            var vm = await VMACSService.Search(result.LoginId);
            PopulateVmacsDetails(result, vm);
        }
    }
}

