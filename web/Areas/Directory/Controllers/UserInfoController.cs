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
using Microsoft.Extensions.Caching.Memory;

namespace Viper.Areas.Directory.Controllers
{
    [Area("Directory")]
    [Permission(Allow = "SVMSecure")]
    public class UserInfoController : AreaController
    {
        public Classes.SQLContext.AAUDContext _aaud;
        private UserInfoService _userInfo;
        public IUserHelper UserHelper;
        private readonly RAPSContext _rapsContext;

        public UserInfoController(RAPSContext rapsContext)
        {
            _aaud = new AAUDContext();
            _rapsContext = rapsContext;
            UserHelper = new UserHelper();
            
            // Manually instantiate UserInfoService following project pattern
            var aaudContext = new AAUDContext();
            var coursesContext = new CoursesContext();
            var equipmentLoanContext = new EquipmentLoanContext();
            var ppsContext = new PPSContext();
            var idCardsContext = new IDCardsContext();
            var keysContext = new KeysContext();
            
            // Get services from DI container
            var httpClientFactory = HttpHelper.HttpContext?.RequestServices.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
            var memoryCache = HttpHelper.HttpContext?.RequestServices.GetService(typeof(IMemoryCache)) as IMemoryCache;
            var configuration = HttpHelper.HttpContext?.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            
            _userInfo = new UserInfoService(
                aaudContext,
                rapsContext,
                coursesContext,
                equipmentLoanContext,
                ppsContext,
                idCardsContext,
                keysContext,
                configuration!,
                httpClientFactory!,
                memoryCache!
            );
        }

        /// <summary>
        /// Redirect if we don't have a mothraID
        /// </summary>
        [Route("/userinfo/")]
        public ActionResult Index()
        {
            return Redirect("/Directory");
        }

        /// <summary>
        /// UserInfo Page
        /// </summary>
        /// <param name="id">MothraID</param>
        /// <returns></returns>
        [Route("/userinfo/{mothraID}")]
        public async Task<ActionResult> UserInfo(string? mothraID)
        {
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(mothraID))
            {
                return Redirect("/Directory");
            }
            else
            {
                // Check if user is viewing their own page
                var currentUser = UserHelper.GetCurrentUser();
                bool ownPage = mothraID == currentUser.MothraId;
                var individual = await _aaud.AaudUsers.Where(u => (u.MothraId == mothraID)).FirstOrDefaultAsync();
                string? iamId = null;
                if (individual != null) iamId = individual.IamId;

                // Get user information
                var userInfo = await _userInfo.GetUserInfoAsync(iamId, mothraID);
                if (userInfo == null)
                {
                    return Redirect("/Directory");
                }

                // Set permissions for the view
                userInfo.CanViewDirectoryDetail = ownPage || UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryDetail");
                userInfo.CanViewStudentID = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.studentID");
                userInfo.CanViewIAM = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.iam");
                userInfo.CanViewRoles = ownPage || UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.raps");
                userInfo.CanViewUCPath = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryUCPathInfo");
                userInfo.CanViewUCPathDetail = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryUCPathInfoAllDetail");
                userInfo.CanViewIDCards = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.idcards");
                userInfo.CanViewKeys = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.keys");
                userInfo.CanViewLoans = ownPage || UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.loans");
                userInfo.CanViewInstinct = ownPage || UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.instinct");
                userInfo.CanViewADGroups = UserHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.UserInfo.ADGroups");

                /*
                userInfo.CanViewDirectoryDetail = true;
                userInfo.CanViewStudentID = true;
                userInfo.CanViewIAM = true;
                userInfo.CanViewRoles = true;
                userInfo.CanViewUCPath = true;
                userInfo.CanViewUCPathDetail = true;
                userInfo.CanViewIDCards = true;
                userInfo.CanViewKeys = true;
                userInfo.CanViewLoans = true;
                userInfo.CanViewInstinct = true;
                userInfo.CanViewADGroups = true;
                */

                return View("~/Areas/Directory/Views/UserInfo.cshtml", userInfo);
            }
        }

        /// <summary>
        /// Get user photo, stubbed for now
        /// </summary>
        /// <param name="mailID">Mail ID</param>
        /// <param name="altphoto">Use alternative photo</param>
        /// <returns></returns>
        [Route("/userPhoto")]
        public async Task<ActionResult> UserPhoto(string mailID, bool altphoto = false)
        {
            return NotFound();
        }

        [Route("/[area]/nav")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav()
        {
            var nav = new List<NavMenuItem>();
            return await Task.Run(() => nav);
        }
    }
}
