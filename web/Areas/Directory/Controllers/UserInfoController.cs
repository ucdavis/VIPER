using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Authorization;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Viper.Areas.Directory.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Areas.CMS.Data;

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
            CoursesContext coursesContext,
            EquipmentLoanContext equipmentLoanContext,
            PPSContext ppsContext,
            IDCardsContext idCardsContext,
            KeysContext keysContext)
        {
            _aaud = aaudContext;
            _rapsContext = rapsContext;
            _userHelper = new UserHelper();

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
        [Route("")]
        public ActionResult Index()
        {
            return Redirect("/Directory");
        }

        /// <summary>
        /// UserInfo Page
        /// </summary>
        /// <param name="id">MothraID</param>
        /// <returns></returns>
        [Route("{mothraID}")]
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
                var currentUser = _userHelper.GetCurrentUser();
                bool ownPage = currentUser != null && mothraID == currentUser.MothraId;
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return NotFound();
        }

        [Route("/[area]/nav")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav()
        {
            var nav = new List<NavMenuItem>();
            return await Task.Run(() => nav);
        }

        [NonAction]
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                                         ActionExecutionDelegate next)
        {
            var viperContext = context.HttpContext.RequestServices.GetRequiredService<VIPERContext>();
            var rapsContext = context.HttpContext.RequestServices.GetRequiredService<RAPSContext>();
            var menu = new LeftNavMenu(viperContext, rapsContext).GetLeftNavMenus(friendlyName: "viper-home")?.FirstOrDefault();
            if (menu != null)
            {
                ConvertNavLinksForDevelopment(menu);
            }
            ViewData["ViperLeftNav"] = menu ?? new NavMenu("", new List<NavMenuItem>());
            await base.OnActionExecutionAsync(context, next);
        }
    }
}
