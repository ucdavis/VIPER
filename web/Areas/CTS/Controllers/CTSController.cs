using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Classes.SQLContext;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Area("CTS")]
    [Route("[area]/[action]")]
    [Authorize(Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class CTSController : AreaController
    {
        private readonly CtsContext _ctsContext;
        public IUserHelper UserHelper;

        public int Count { get; set; }
        public string? UserName { get; set; }

        public CTSController(CtsContext context, IWebHostEnvironment environment)
        {
            _ctsContext = context;
            UserHelper = new UserHelper();
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
            await next();
            ViewData["ViperLeftNav"] = Nav();
        }

        public NavMenu Nav()
        {
            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Home", MenuItemURL = "Home" }
            };

            return new NavMenu("Competency Tracking System", nav);
        }

        [Route("/[area]")]
        public IActionResult Index()
        {
            return View("~/Areas/CTS/Views/Index.cshtml");
        }

        [Route("/[area]/[action]")]
        public IActionResult Epa()
        {
            ViewData["VIPERLayout"] = "VIPERLayoutSimplified";

            return View("~/Areas/CTS/Views/Epa.cshtml");
        }
    }
}
