using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Models.RAPS;
using Viper.Areas.RAPS.Services;
using Web.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Viper.Areas.RAPS.Controllers
{
    //TODO: Create the RAPS Delegate Users role and add anyone with access to delegate roles
    [Area("RAPS")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Delegate Users", Policy = "2faAuthentication")]
    public class RAPSController : Controller
    {
        private readonly Classes.SQLContext.RAPSContext _RAPSContext;
        private RAPSSecurityService _securityService;

        public int Count { get; set; }
        public string? UserName { get; set; }

        public RAPSController(Classes.SQLContext.RAPSContext context)
        {
            _RAPSContext = context;
            _securityService = new RAPSSecurityService(context);
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


        /// <summary>
        /// RAPS Role List. Will show ListAdmin or List view.
        /// Open to admins, IT people for VMACS roles, and Role "owners" for their roles.
        /// </summary>
        /// <param name="Instance">RAPS Instance</param>
        /// <returns></returns>
        [Route("/[area]/{Instance=VIPER}/[action]")]
        public async Task<IActionResult> RoleList(string Instance="VIPER")
        {
            ViewData["Instance"] = Instance.ToUpper();
            
            if (UserHelper.HasPermission(_RAPSContext, UserHelper.GetCurrentUser(), "RAPS.Admin"))
            {
                return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/ListAdmin.cshtml"));
            }
            else if(_securityService.IsAllowedTo("ViewAllRoles", Instance) ||
                    !_securityService.GetControlledRoleIds(UserHelper.GetCurrentUser()?.MothraId).IsNullOrEmpty())
            {
                return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/List.cshtml"));
            }
            else
            {
                //TODO: Should probably have a deny access helper function that writes logs and sets view
                return await Task.Run(() => View("~/Views/Home/403.cshtml"));
            }
        }

        /// <summary>
        /// Role members. Open to admins, IT people for VMACS roles, and Role "owners" for their roles.
        /// </summary>
        /// <param name="Instance">RAPS Instance</param>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        [Route("/[area]/{Instance=VIPER}/[action]")]
        public async Task<IActionResult> RoleMembers(string Instance, int RoleId, int v=1)
        {
            ViewData["RoleId"] = RoleId;

            TblRole? Role = await _RAPSContext.TblRoles.FindAsync(RoleId);

            if (Role == null)
            {
                return NotFound();
            }
            if (_securityService.IsAllowedTo("EditRoleMembers", Instance, Role))
            { 
                return v ==1 ? View("~/Areas/RAPS/Views/Roles/Members.cshtml")
                        : View("~/Areas/RAPS/Views/Roles/Members2.cshtml");
            }
            else
            {
                //TODO: Should probably have a deny access helper function that writes logs and sets view
                return View("~/Views/Home/403.cshtml");
            }
        }

        /// <summary>
        /// Edit a role.
        /// </summary>
        /// <param name="Instance">RAPS Instance</param>
        /// <param name="RoleId">ID of role to edit</param>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin")]
        [Route("/[area]/{Instance=VIPER}/[action]/{RoleId?}")]
        public async Task<IActionResult> RoleEdit(int? RoleId)
        {
            ViewData["RoleId"] = RoleId;

            List<TblRole> Roles = await _RAPSContext.TblRoles
                    .Include(r => r.TblRoleMembers)
                    .ThenInclude(rm => rm.AaudUser)
                    .Where(r => r.RoleId == RoleId).ToListAsync();
            return View("~/Areas/RAPS/Views/Roles/Edit.cshtml");
        }


        /// <summary>
        /// List permissions
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        [Route("/[area]/{Instance=VIPER}/[action]")]
        public async Task<IActionResult> PermissionList()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Permissions/List2.cshtml"));
        }
    }
}
