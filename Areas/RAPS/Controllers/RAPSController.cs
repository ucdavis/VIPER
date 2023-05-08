using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Views.Shared.Components.VueTableDefault;

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
            ViewData["KeyColumnName"] = "RoleId";

            var data = await _RAPSContext.TblRoles.ToListAsync();
            //var skipList = new List<string> { "Description" };
            //var altColumnNames = new List<Tuple<string, string>> { new Tuple<string, string>("ViewName","Viewer") };

            //ViewData["Columns"] = VueTableDefaultViewComponent.GetDefaultColumnNames(data, skipList, altColumnNames);
            //ViewData["Rows"] = VueTableDefaultViewComponent.GetDefaultRows(data, skipList);
            //ViewData["VisibleColumns"] = VueTableDefaultViewComponent.GetDefaultVisibleColumns(data, skipList);

            return _RAPSContext.TblRoles != null ?
                        View("~/Areas/RAPS/Views/Index.cshtml", data) :
                        Problem("Entity set 'RAPSContext.TblRoles'  is null.");
        }


        /*Testing Role view/edit functions with client side UI*/

        [Route("/[area]/[action]")]
        public async Task<IActionResult> Roles()
        {
            return View("~/Areas/RAPS/Views/Roles/List.cshtml");
        }

        [Route("/[area]/[action]/{RoleId?}")]
        public async Task<IActionResult> RoleEdit(int? RoleId)
        {
            ViewData["RoleId"] = RoleId;
            return View("~/Areas/RAPS/Views/Roles/Edit.cshtml");
        }

        [Route("/[area]/[action]")]
        public async Task<IActionResult> RoleMembers(int RoleId)
        {
            ViewData["RoleId"] = RoleId;
            return View("~/Areas/RAPS/Views/Roles/Members.cshtml");
        }
    }
}
