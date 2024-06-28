using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Classes.SQLContext;
using Viper.Classes;
using Web.Authorization;
using Microsoft.EntityFrameworkCore;
using NLog;
using Viper.Areas.CTS.Services;

namespace Viper.Areas.CTS.Controllers
{
    [Area("CTS")]
    [Route("[area]/[action]")]
    [Authorize(Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class CTSController : AreaController
    {
        private readonly VIPERContext _viperContext;
        private readonly RAPSContext _rapsContext;
        private readonly CtsSecurityService ctsSecurityService;
        private readonly IWebHostEnvironment environment;
        public IUserHelper UserHelper;

        public CTSController(VIPERContext context, RAPSContext rapsContext, IWebHostEnvironment env)
        {
            _viperContext = context;
            _rapsContext = rapsContext;
            ctsSecurityService = new CtsSecurityService(rapsContext, _viperContext);
            UserHelper = new UserHelper();
            environment = env;
        }

        /// <summary>
        /// Getting left nav for each page. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                         ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);
            ViewData["ViperLeftNav"] = Nav();
        }

        public NavMenu Nav()
        {
            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "CTS Home", MenuItemURL = "" },
                new NavMenuItem() { MenuItemText = "Assessments", IsHeader = true }
            };

            if (UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.AssessClinical")
                || UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.Manage"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "EPA Assessment", MenuItemURL = "~/CTS/EPA" });
            }

            //All assessments, or assessments the logged in user has created
            if (UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.Manage")
                || UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.StudentAssessments")
                || UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.AssessClinical"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "View Assessments", MenuItemURL = "~/CTS/Assessments" });
            }
            //Assessments of the logged in user
            if (UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.Students"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "My Assessments", MenuItemURL = "~/CTS/MyAssessments" });
            }

            if (UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.Manage"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Admin Functions", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Domains", MenuItemURL = "~/CTS/ManageDomains" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Competencies", MenuItemURL = "~/CTS/ManageCompetencies" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage Levels", MenuItemURL = "~/CTS/ManageLevels" });
                nav.Add(new NavMenuItem() { MenuItemText = "Manage EPAs", MenuItemURL = "~/CTS/ManageEPAs" });

                nav.Add(new NavMenuItem() { MenuItemText = "Reports", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Assessment Charts", MenuItemURL = "~/CTS/AssessmentCharts" });
            }

            return new NavMenu("Competency Tracking System", nav);
        }

        [Route("/[area]")]
        public IActionResult Index()
        {
           
            return View("~/Areas/CTS/Views/Index.cshtml");
        }

    }
}
