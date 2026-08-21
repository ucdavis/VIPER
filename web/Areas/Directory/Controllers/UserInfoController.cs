using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Authorization;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Areas.Directory.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Viper.Areas.Directory.Controllers
{
    [Area("Directory")]
    [Permission(Allow = "SVMSecure")]
    [Route("userinfo")]
    public class UserInfoController : AreaController
    {
        private readonly AAUDContext _aaud;
        private readonly UserInfoService _userInfo;
        private readonly IUserHelper _userHelper;
        private readonly RAPSContext _rapsContext;

        public UserInfoController(
            RAPSContext rapsContext,
            AAUDContext aaudContext,
            UserInfoService userInfo)
        {
            _aaud = aaudContext;
            _rapsContext = rapsContext;
            _userHelper = new UserHelper();
            _userInfo = userInfo;
        }

        /// <summary>
        /// Redirect if we don't have a mothraID
        /// </summary>
        [Route("")]
        public ActionResult Index()
        {
            return Redirect("~/Directory");
        }

        /// <summary>
        /// UserInfo Page
        /// </summary>
        /// <param name="mothraID">MothraID</param>
        /// <returns></returns>
        [Route("{mothraID}")]
        public async Task<ActionResult> UserInfo(string? mothraID)
        {
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(mothraID))
            {
                return Redirect("~/Directory");
            }
            else
            {
                // Check if user is viewing their own page
                var currentUser = _userHelper.GetCurrentUser();
                bool ownPage = currentUser != null && mothraID == currentUser.MothraId;
                var individual = await _aaud.AaudUsers.FirstOrDefaultAsync(u => (u.MothraId == mothraID));
                string? iamId = null;
                if (individual != null) iamId = individual.IamId;

                // Get user information
                var userInfo = await _userInfo.GetUserInfoAsync(iamId, mothraID);
                if (userInfo == null)
                {
                    return Redirect("~/Directory");
                }

                // Set permissions for the view
                userInfo.CanViewDirectoryDetail = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryDetail");
                userInfo.CanViewStudentID = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.studentID");
                userInfo.CanViewIAM = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.iam");
                userInfo.CanViewRoles = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.raps");
                userInfo.CanViewUCPath = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryUCPathInfo");
                userInfo.CanViewUCPathDetail = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryUCPathInfoAllDetail");
                userInfo.CanViewIDCards = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.idcards");
                userInfo.CanViewKeys = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.keys");
                userInfo.CanViewLoans = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.loans");
                userInfo.CanViewInstinct = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.instinct");
                userInfo.CanViewADGroups = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.UserInfo.ADGroups");
                return View("~/Areas/Directory/Views/UserInfo.cshtml", userInfo);
            }
        }

        [Route("nav")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav()
        {
            var nav = new List<NavMenuItem>();
            return await Task.Run(() => nav);
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
