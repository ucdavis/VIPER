using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;
using Viper.Classes.SQLContext;
using Viper.Classes;
using Viper.Models.RAPS;
using Viper.Areas.RAPS.Services;

namespace Viper.Areas.CTS.Controllers
{
    [Area("RAPS")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Users", Policy = "2faAuthentication")]
    public class CTSController : AreaController
    {
        private readonly Classes.SQLContext.CtsContext _ctsContext;
        public IUserHelper UserHelper;

        public int Count { get; set; }
        public string? UserName { get; set; }

        public CTSController(Classes.SQLContext.CtsContext context, IWebHostEnvironment environment)
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
            ViewData["ViperLeftNav"] = await Nav();
        }

        public async Task<NavMenu> Nav()
        {
            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Home", MenuItemURL = "Home" }
            };

            return new NavMenu("Competency Tracking System", nav);
        }

        public IActionResult Home()
        {
            return View();
        }
    }
}
