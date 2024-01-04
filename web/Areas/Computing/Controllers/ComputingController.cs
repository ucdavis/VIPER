using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Areas.CMS.Data;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Computing.Controllers
{
    [Area("Computing")]
    [Route("[area]/[action]")]
    [Authorize(Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure")]
    public class ComputingController : AreaController
    {
        private readonly IWebHostEnvironment _environment;
        public IUserHelper UserHelper;

        public int Count { get; set; }
        public string? UserName { get; set; }

        public ComputingController(IWebHostEnvironment environment)
        {
            _environment = environment;
            UserHelper = new UserHelper();
        }

        [Route("/[area]")]
        [Authorize(Policy = "2faAuthentication")]
        [Permission(Allow = "SVMSecure")]
        public IActionResult Index()
        {
            return View("~/Areas/Computing/Views/Index.cshtml");
        }

        [Authorize(Policy = "2faAuthentication")]
        [Permission(Allow = "SVMSecure")]
        public IActionResult Test()
        {
            return View();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            ViewData["ViperLeftNav"] = Nav();
        }

        private NavMenu Nav()
        {
            var menu = new LeftNavMenu().GetLeftNavMenus(friendlyName: "viper-cats")?.FirstOrDefault();
            if (menu != null)
            {
                ConvertNavLinksForDevelopment(menu);
            }
            return menu ?? new NavMenu("", new List<NavMenuItem>());
        }
    }
}
