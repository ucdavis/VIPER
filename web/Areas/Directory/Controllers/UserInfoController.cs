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
    [Area("UserInfo")]
    [Permission(Allow = "SVMSecure")]
    [Authorize(Roles = "VMDO SVM-IT")] //locking directory for now until it's complete
    public class UserInfoController : AreaController
    {
        public Classes.SQLContext.AAUDContext _aaud;
        private readonly RAPSContext? _rapsContext;
        public IUserHelper UserHelper;

        public UserInfoController(Classes.SQLContext.RAPSContext context)
        {
        _aaud = new AAUDContext();
        this._rapsContext = (RAPSContext?)HttpHelper.HttpContext?.RequestServices.GetService(typeof(RAPSContext));
        UserHelper = new UserHelper();
        }

        /// <summary>
        /// UserInfo without a MothraID
        /// </summary>
        [Route("/[area]/")]
        public async Task<ActionResult> Index(string? useExample)
        {
            return Redirect("/directory");
        }


        /// <summary>
        /// UserInfo page
        /// </summary>
        /// <param name="mothraID">Mothra ID</param>
        /// <returns></returns>
        [SupportedOSPlatform("windows")]
        [Route("/[area]/{MothraID}")]
        public async Task<ActionResult<IEnumerable<UserInfoResult>>> Get(string MothraID)
        {
            var individuals = await _aaud.AaudUsers
                     .Where(u => (u.MothraId == MothraID)
                )
                .Where(u => u.Current != 0)
                .ToListAsync();
            List<UserInfoResult> results = new();
            AaudUser? currentUser = UserHelper.GetCurrentUser();
            bool hasDetailPermission = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.DirectoryDetail");
            individuals.ForEach(m =>
            {
                LdapUserContact? l = new LdapService().GetUserByID(m.IamId);
                results.Add(hasDetailPermission
                        ? new UserInfoResultComplete(m, l)
                        : new UserInfoResult(m, l));

                var vmsearch = VMACSService.Search(results.Last().LoginId);
                var vm = vmsearch.Result;
                if (vm != null && vm.item != null && vm.item.Nextel != null) results.Last().Nextel = vm.item.Nextel[0];
                if (vm != null && vm.item != null && vm.item.LDPager != null) results.Last().Pager = vm.item.LDPager[0];
                if (vm != null && vm.item != null && vm.item.Unit != null) results.Last().Department = vm.item.Unit[0];
            });
            return await Task.Run(() => View("~/Areas/Directory/Views/UserInfo.cshtml", results.First()));
        }
    }
}
