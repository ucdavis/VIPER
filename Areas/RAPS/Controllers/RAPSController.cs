using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Viper.Areas.RAPS.Controllers
{
    [Area("RAPS")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "IT Leadership & Supervisors,ITS_Operations,ITS_Programmers,VMDO CATS-Programmers,VMDO CATS-Techs,VMDO SVM-IT", Policy = "2faAuthentication")]
    public class RAPSController : Controller
    {
        private readonly Classes.SQLContext.RAPSContext _RAPSContext;

        public int Count { get; set; }
        public string? UserName { get; set; }

        public RAPSController(Classes.SQLContext.RAPSContext context)
        {
            _RAPSContext = context;
        }

        /// <summary>
        /// RAPS home page
        /// </summary>
        [Route("/[area]")]
        public async Task<IActionResult> Index()
        {
            int? tempCount = HttpContext.Session.GetInt32("RAPSCounter");

            if (tempCount != null)
            {
                Count = tempCount.Value;
            }
            else
            {
                Count = 0;
            }

            UserName = User?.Identity?.Name;

            ViewData["Count"] = Count;
            ViewData["UserName"] = UserName;

            return _RAPSContext.TblRoles != null ?
                        View("~/Areas/RAPS/Views/Index.cshtml", await _RAPSContext.TblRoles.ToListAsync()) :
                        Problem("Entity set 'RAPSContext.TblRoles'  is null.");
        }

        [Route("/[area]/[action]/{counter}")]
        [HttpPost]
        public void UpdateCounter(int counter)
        {
            ViewData["Count"] = counter;
            HttpContext.Session.SetInt32("RAPSCounter", counter);
        }
    }
}
