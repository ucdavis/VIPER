using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Models.AAUD;
using Viper.Areas.RAPS.Services;
using Web.Authorization;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Areas.Directory.Models;
using System.Runtime.Versioning;
using System.Collections.Generic;
using Viper.Areas.Directory.Services;
using Viper.Classes.Utilities;

namespace Viper.Areas.Directory.Controllers
{
    [Area("Directory")]
    [Permission(Allow = "SVMSecure")]
    [Authorize(Roles = "VMDO SVM-IT")] //locking directory for now until it's complete
    public class DirectoryController : AreaController
    {
        public Classes.SQLContext.AAUDContext _aaud;
        public Models.DirectoryUser User;
        private readonly RAPSContext? _rapsContext;
        public IUserHelper UserHelper;

        public DirectoryController(Classes.SQLContext.RAPSContext context)
        {
            _aaud = new AAUDContext();
            this._rapsContext = (RAPSContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(RAPSContext));
            UserHelper = new UserHelper();
            User = new DirectoryUser();
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("/[area]/")]
        public async Task<ActionResult> Index(string? useExample)
        {
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            User.CanDisplayIDs = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail");
            User.CanEmulate = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.SU");

            return await Task.Run(() => View("~/Areas/Directory/Views/Card.cshtml",User));
        }

        /// <summary>
        /// Directory home page
        /// </summary>
        [Route("/[area]/nav")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav()
        {
            var nav = new List<NavMenuItem>
            {
            };
            return await Task.Run(() => nav);
        }


        /// <summary>
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        [Route("/[area]/search/{search}")]
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
                LdapUserContact? l = new LdapService().GetUserByID(m.IamId);
                results.Add(hasDetailPermission
                    ? new IndividualSearchResultWithIDs(m, l)
                    : new IndividualSearchResult(m, l));

                var vmsearch = VMACSService.Search(results.Last().LoginId);
                var vm = vmsearch.Result;
                if (vm != null && vm.item != null && vm.item.Nextel != null) results.Last().Nextel = vm.item.Nextel[0];
                if (vm != null && vm.item != null && vm.item.LDPager != null) results.Last().LDPager = vm.item.LDPager[0];
                if (vm != null && vm.item != null && vm.item.Unit != null) results.Last().Department = vm.item.Unit[0];

            });
            return results;
        }

        /// <summary>
        /// Directory list
        /// </summary>
        /// <param name="search">search string</param>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        [Route("/[area]/search/{search}/ucd")]
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
                results.Add(hasDetailPermission
                    ? new IndividualSearchResultWithIDs(userInfo, l)
                    : new IndividualSearchResult(userInfo, l));

                var vmsearch = VMACSService.Search(results.Last().LoginId);
                var vm = vmsearch.Result;
                if (vm != null && vm.item != null && vm.item.Nextel != null) results.Last().Nextel = vm.item.Nextel[0];
                if (vm != null && vm.item != null && vm.item.LDPager != null) results.Last().LDPager = vm.item.LDPager[0];
                if (vm != null && vm.item != null && vm.item.Unit != null) results.Last().Department = vm.item.Unit[0];
            };
            return results;
        }
    }
}
