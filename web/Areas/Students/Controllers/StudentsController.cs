using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Areas.CMS.Data;
using Viper.Classes;
using Web.Authorization;

namespace Viper.Areas.Students.Controllers
{
    [Area("Students")]
    [Route("[area]/[action]")]
    [Authorize(Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure.Students")]
    public class StudentsController : AreaController
    {
        public StudentsController()
        {
        }

        /// <summary>
        /// Getting left nav for each page.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
#pragma warning disable S6967 // Lifecycle override, not a user-facing action endpoint
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                         ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);
            ViewData["ViperLeftNav"] = Nav();
        }
#pragma warning restore S6967

        public NavMenu Nav()
        {
            var menu = new LeftNavMenu().GetLeftNavMenus(friendlyName: "viper-students")?.FirstOrDefault();
            if (menu != null)
            {
                ConvertNavLinksForDevelopment(menu);
            }
            return menu ?? new NavMenu("", new List<NavMenuItem>());
        }

        [Route("/[area]")]
        public IActionResult Index()
        {
            return View("~/Areas/Students/Views/Index.cshtml");
        }

        [Route("/[area]/[action]")]
#pragma warning disable S6967 // View-only action with query params, no model binding validation needed
        public IActionResult StudentClassYear(string? import = null, int? classYear = null)
        {
            return import != null
                ? View("~/Areas/Students/Views/StudentClassYearImport.cshtml")
                : View("~/Areas/Students/Views/StudentClassYear.cshtml");
        }
#pragma warning restore S6967

        [Route("/[area]/[action]")]
        public IActionResult StudentClassYearreport()
        {
            return View("~/Areas/Students/Views/StudentClassYearReport.cshtml");
        }

    }
}
