using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viper.Classes.SQLContext;
using Viper.Classes;
using Web.Authorization;
using Microsoft.EntityFrameworkCore;
using NLog;

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
        public IUserHelper UserHelper;

        public CTSController(VIPERContext context, RAPSContext rapsContext)
        {
            _viperContext = context;
            _rapsContext = rapsContext;
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
            ViewData["ViperLeftNav"] = Nav();
        }

        public NavMenu Nav()
        {
            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "CTS Home", MenuItemURL = "" }
			};

            if(UserHelper.HasPermission(_rapsContext, UserHelper.GetCurrentUser(), "SVMSecure.CTS.Manage"))
            {
				nav.Add(new NavMenuItem() { MenuItemText = "Admin Functions", IsHeader = true });
				nav.Add(new NavMenuItem() { MenuItemText = "Manage Domains", MenuItemURL = "ManageDomains" });
				nav.Add(new NavMenuItem() { MenuItemText = "Manage Competencies", MenuItemURL = "ManageCompetencies" });
				nav.Add(new NavMenuItem() { MenuItemText = "Manage Levels", MenuItemURL = "ManageLevels" });
				nav.Add(new NavMenuItem() { MenuItemText = "Manage EPAs", MenuItemURL = "ManageEPAs" });
			}

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

		[Permission(Allow = "SVMSecure.CTS.Manage")]
		public async Task<ActionResult> ManageDomains(int? domainId)
        {
            if(domainId != null)
            {
                ViewData["Domain"] = await _viperContext.Domains.FindAsync(domainId);
            }
            ViewData["Domains"] = await _viperContext.Domains.ToListAsync();
            return View("~/Areas/CTS/Views/ManageDomains.cshtml");
		}
		
		[HttpPost]
		[Permission(Allow = "SVMSecure.CTS.Manage")]
		public async Task<ActionResult> ManageDomains(int domainId, string name, int order, string description) {
            Logger l = LogManager.GetCurrentClassLogger();
            l.Warn("In Domain Save");

			List<string> errors = new();
            if(string.IsNullOrEmpty(name))
            {
                errors.Add("Domain Name is required");
            }

            if(domainId > 0 && errors.Count == 0)
            {
                var existing = _viperContext.Domains.Find(domainId);
                if(existing == null)
                {
                    errors.Add("Domain not found.");
                }
                else
                {
                    existing.Name = name;
                    existing.Order = order;
                    existing.Description = description;
                    _viperContext.Entry(existing).State = EntityState.Modified;
                }
            }
            else if (errors.Count == 0)
            {
                l.Warn("Saving!");
                var newDomain = new Viper.Models.CTS.Domain() { Name = name, Order = order, Description = description };
				_viperContext.Domains.Add(newDomain);
			}
			await _viperContext.SaveChangesAsync();

			ViewData["Errors"] = errors;

            return errors.Count == 0 ? Redirect("~/CTS/ManageDomains") : View("~/Areas/CTS/Views/ManageDomains.cshtml");
		}

		[Permission(Allow = "SVMSecure.CTS.Manage")]
		public IActionResult ManageLevels()
        {
            return View("~/Areas/CTS/Views/ManageLevels.cshtml");
        }

        public IActionResult Epa()
        {
            ViewData["VIPERLayout"] = "VIPERLayoutSimplified";

            return View("~/Areas/CTS/Views/Epa.cshtml");
        }
    }
}
