using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Authorization;
using Viper.Areas.Directory.Models;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Areas.Directory.Services;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Viper.Areas.Directory.Controllers
{
    [Area("Directory")]
    [Permission(Allow = "SVMSecure.userinfo")]
    [Authorize(Roles = "VMDO SVM-IT")] //locking directory for now until it's complete
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
        [Route("/Directory/userInfo/{mothraID}")]
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

                // Compute permissions before fetching so UserInfoService can skip populating
                // sections this requester isn't allowed to see, instead of fetching everything
                // and only gating the view.
                var permissions = new UserInfoViewPermissions
                {
                    IsOwnPage = ownPage,
                    CanViewDirectoryDetail = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryDetail"),
                    CanViewStudentID = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.studentID"),
                    CanViewIAM = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.iam"),
                    CanViewRoles = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.raps"),
                    CanViewUCPath = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryUCPathInfo"),
                    CanViewUCPathDetail = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.directoryUCPathInfoAllDetail"),
                    CanViewIDCards = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.idcards"),
                    CanViewKeys = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.keys"),
                    CanViewLoans = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.loans"),
                    CanViewInstinct = ownPage || _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.userinfo.instinct"),
                    CanViewADGroups = _userHelper.HasPermission(_rapsContext, currentUser, "SVMSecure.UserInfo.ADGroups")
                };

                var individual = await _aaud.AaudUsers.AsNoTracking().FirstOrDefaultAsync(u => (u.MothraId == mothraID));
                string? iamId = null;
                if (individual != null) iamId = individual.IamId;

                // Get user information
                var userInfo = await _userInfo.GetUserInfoAsync(iamId, mothraID, permissions);
                if (userInfo == null)
                {
                    return Redirect("~/Directory");
                }

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
