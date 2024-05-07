using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Classes.SQLContext;
using Viper.Classes;
using Web.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Viper.Areas.CTS.Controllers
{
    [Area("CTS")]
    [Route("[area]/[action]")]
    [Authorize(Policy = "2faAuthentication")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class CTSController : AreaController
    {
        private readonly VIPERContext _viperContext;
        public IUserHelper UserHelper;

        public CTSController(VIPERContext context)
        {
            _viperContext = context;
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
            var levels = _viperContext.Levels.ToList();
            var epas = _viperContext.Epas.ToList();
            var encounters = _viperContext.Encounters
                .Include(e => e.EncounterInstructors)
                .Include(e => e.Clinician)
                .Include(e => e.Student)
                .Include(e => e.Service)
                .Include(e => e.Offering)
                .Include(e => e.EnteredByPerson)
                .ToList();
            var studentEpas = _viperContext.StudentEpas
                .Include(e => e.Encounter)
                .Include(e => e.Epa)
                .Include(e => e.Level)
                .ToList();
            var people = _viperContext.People.ToList();
            
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
