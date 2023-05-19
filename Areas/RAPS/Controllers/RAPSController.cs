using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Models.RAPS;
using Viper.Areas.RAPS.Services;
using Web.Authorization;
using Microsoft.IdentityModel.Tokens;
using Viper.Classes;

namespace Viper.Areas.RAPS.Controllers
{
    //TODO: Create the RAPS Delegate Users role and add anyone with access to delegate roles
    [Area("RAPS")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Delegate Users", Policy = "2faAuthentication")]
    public class RAPSController : AreaController
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
        [Route("/[area]/{instance?}")]
        public async Task<ActionResult> Index(string? instance)
        {
            ViewData["KeyColumnName"] = "RoleId";
            if (instance == null)
            {
                instance = _securityService.GetDefaultInstanceForUser();
            }

            return await Task.Run(() => Redirect(string.Format("/raps/{0}/rolelist", instance)));

            //var data = await _RAPSContext.TblRoles.ToListAsync();
            //var skipList = new List<string> { "Description" };
            //var altColumnNames = new List<Tuple<string, string>> { new Tuple<string, string>("ViewName","Viewer") };

            //ViewData["Columns"] = VueTableDefaultViewComponent.GetDefaultColumnNames(data, skipList, altColumnNames);
            //ViewData["Rows"] = VueTableDefaultViewComponent.GetDefaultRows(data, skipList);
            //ViewData["VisibleColumns"] = VueTableDefaultViewComponent.GetDefaultVisibleColumns(data, skipList);

            //return _RAPSContext.TblRoles != null ?
            //            View("~/Areas/RAPS/Views/Index.cshtml", data) :
            //            Problem("Entity set 'RAPSContext.TblRoles'  is null.");}
        }

        [Route("/[area]/{instance}/[action]")]
        public async Task<ActionResult<IEnumerable<NavMenuItem>>> Nav(int? roleId, int? permissionId, string? memberId, string instance = "VIPER")
        {
            TblRole? selectedRole = (roleId != null) ? await _RAPSContext.TblRoles.FindAsync(roleId) : null;
            TblPermission? selectedPermission = (permissionId != null) ? await _RAPSContext.TblPermissions.FindAsync(permissionId) : null;
            VwAaudUser? selecteduser = (memberId != null) ? _RAPSContext.VwAaudUser.Single(r => r.MothraId == memberId) : null;
           
            var nav = new List<NavMenuItem>();
            //Links to instances
            nav.Add(new NavMenuItem() { MenuItemText = "Instances", IsHeader = true });
            foreach (string inst in (new[] { "Viper", "ViperForms", "VMACS.VMTH", "VMACS.VMLF", "VMACS.UCVMCSD" }))
            {
                if(_securityService.IsAllowedTo("AccessInstance", inst))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = inst, MenuItemURL = "/raps/" + inst });
                }
            }
            nav.Add(new NavMenuItem() { MenuItemText = "Roles", IsHeader = true });
            nav.Add(new NavMenuItem() { MenuItemText = "Role List", MenuItemURL = "rolelist" });
            if(_securityService.IsAllowedTo("CreateRole", instance))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Role Templates", MenuItemURL = "roleTemplates" });
            }
            if (selectedRole != null && _securityService.RoleBelongsToInstance(instance, selectedRole))
            {
                nav.Add(new NavMenuItem() { MenuItemText = string.Format("Selected Role {0}", selectedRole.Role), IsHeader = true });
                if (selectedRole.Application == 0 && _securityService.IsAllowedTo("EditPermissions"))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = "Edit Permissions", MenuItemURL = "rolePermisisons" });
                }
                if(_securityService.IsAllowedTo("EditRoleMembership"))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = "Role Members", MenuItemURL = "roleMembers" });
                }
                
            }
            if(_securityService.IsAllowedTo("ManageAllPermissions") || _securityService.IsAllowedTo("ViewPermissions"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Permissions", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Permission List", MenuItemURL = "permissionlist" });
                if(selectedPermission != null)
                {
                    nav.Add(new NavMenuItem() { MenuItemText = string.Format("Selected Permission {0}", selectedPermission.Permission), IsHeader = true });
                    if(_securityService.IsAllowedTo("EditRolePermissions"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "Roles w/ Permission", MenuItemURL = "permissionRoles" });
                    }
                    if (_securityService.IsAllowedTo("EditMemberPermissions"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "Users w/ Permission", MenuItemURL = "permissionMembers" });
                    }
                }
            }
            if(_securityService.IsAllowedTo("UserLookup"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Users", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "User Lookup", MenuItemURL = "userSearch" });
                if(_securityService.IsAllowedTo("Clone", instance))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = "Clone User Permissions", MenuItemURL = "userClone" });
                }
                if(selecteduser != null)
                {
                    nav.Add(new NavMenuItem() { MenuItemText = string.Format("Selected User {0}", selecteduser.DisplayFullName), IsHeader = true });
                    if(_securityService.IsAllowedTo("EditRoleMembership"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "User Roles", MenuItemURL = "memberRoles" });
                    }
                    if (_securityService.IsAllowedTo("EditMemberPermissions"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "User Permissions", MenuItemURL = "memberPermissions" });
                    }
                    if (_securityService.IsAllowedTo("RSOP"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "Combined Permissions", MenuItemURL = "memberRSOP" });
                    }
                }
            }
            if(_securityService.IsAllowedTo("ViewAuditTrail"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Admin", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "View Audit Trail", MenuItemURL = "auditTrail" });
            }
            return nav;
        }


        /// <summary>
        /// RAPS Role List. Will show ListAdmin or List view.
        /// Open to admins, IT people for VMACS roles, and Role "owners" for their roles.
        /// </summary>
        /// <param name="Instance">RAPS Instance</param>
        /// <returns></returns>
        [Route("/[area]/{instance}/[action]")]
        public async Task<IActionResult> RoleList(string instance)
        {
            ViewData["instance"] = instance.ToUpper();
            
            if (UserHelper.HasPermission(_RAPSContext, UserHelper.GetCurrentUser(), "RAPS.Admin"))
            {
                return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/ListAdmin.cshtml"));
            }
            else if(_securityService.IsAllowedTo("ViewAllRoles", instance) ||
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
        [Route("/[area]/{instance}/[action]")]
        public async Task<IActionResult> RoleMembers(string instance, int RoleId, int v=1)
        {
            ViewData["RoleId"] = RoleId;

            TblRole? Role = await _RAPSContext.TblRoles.FindAsync(RoleId);

            if (Role == null)
            {
                return NotFound();
            }
            if (_securityService.IsAllowedTo("EditRoleMembers", instance, Role))
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
        [Route("/[area]/{Instance}/[action]/{RoleId?}")]
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
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> PermissionList()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Permissions/List.cshtml"));
        }
    }
}
