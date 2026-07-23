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

namespace Viper.Areas.Directory.Controllers
{
    [Area("Directory")]
    [Route("/[area]")]
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
        public ActionResult Index(string? useExample)
        {
            return View("~/Areas/Directory/Views/Card.cshtml");
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
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        [SupportedOSPlatform("windows")]
        [Route("search/{search}")]
        public async Task<ActionResult<IEnumerable<IndividualSearchResult>>> Get(string search)
        {
            var individuals = await SearchCurrentAaudUsers(_aaud, search);
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
            var individuals = await SearchCurrentAaudUsers(_aaud, search);
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

        /// <summary>
        /// Directory results
        /// </summary>
        /// <param name="uid">User ID</param>
        [Route("userInfo/{mothraID}")]
        public IActionResult DirectoryResult(string mothraID)
        {
            // pull in the user based on uid
            return View("~/Areas/Directory/Views/UserInfo.cshtml");
        }

        /// <summary>
        /// Current AAUD users matching the search term on name or any directory identifier,
        /// ordered for display. Shared by Get and GetUCD.
        /// </summary>
        internal static Task<List<AaudUser>> SearchCurrentAaudUsers(AAUDContext aaud, string search)
        {
            return aaud.AaudUsers
                .AsNoTracking()
                .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                    || new[] { u.MailId, u.LoginId, u.SpridenId, u.Pidm, u.MothraId, u.EmployeeId, u.IamId }
                        .Any(id => id != null && id.Contains(search)))
                .Where(u => u.Current != 0)
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
            var item = (await VMACSService.Search(result.LoginId))?.item;
            if (item == null)
            {
                return;
            }
            if (item.Nextel is { Length: > 0 }) result.Nextel = item.Nextel[0];
            if (item.LDPager is { Length: > 0 }) result.LDPager = item.LDPager[0];
            if (item.Unit is { Length: > 0 }) result.Department = item.Unit[0];
        }
    }
}
