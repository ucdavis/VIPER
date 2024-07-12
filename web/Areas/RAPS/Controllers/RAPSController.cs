using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Viper.Models.RAPS;
using Viper.Areas.RAPS.Services;
using Web.Authorization;
using Microsoft.IdentityModel.Tokens;
using Viper.Classes;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;
using Viper.Classes.Utilities;

namespace Viper.Areas.RAPS.Controllers
{
    [Area("RAPS")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "VMDO SVM-IT,RAPS Users")]//, Policy = "2faAuthentication"
    public class RAPSController : AreaController
    {
        private readonly Classes.SQLContext.RAPSContext _RAPSContext;
        private readonly RAPSSecurityService _securityService;
        private readonly IWebHostEnvironment _environment;
		public IUserHelper UserHelper;

        public int Count { get; set; }
        public string? UserName { get; set; }

        public RAPSController(Classes.SQLContext.RAPSContext context, IWebHostEnvironment environment)
        {
            _RAPSContext = context;
            _securityService = new RAPSSecurityService(context);
            _environment = environment;
            UserHelper = new UserHelper();
        }

        /// <summary>
        /// Getting left nav for each page. This is a little complicated - alternatively, ViewData["ViperLeftNav"] = await Nav() 
        /// could be added to each action.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context,
                                         ActionExecutionDelegate next)
        {
            await base.OnActionExecutionAsync(context, next);
            await next();
            bool roleIdValid = int.TryParse(HttpContext?.Request?.Query["roleId"].FirstOrDefault(), out int roleId);
            bool permIdValid = int.TryParse(HttpContext?.Request?.Query["permissionId"].FirstOrDefault(), out int permissionId);
            string? memberId = HttpContext?.Request?.Query["memberId"].FirstOrDefault();
            List<string>? path = HttpContext?.Request?.Path.ToString().Split("/").ToList();
            int? rapsIdx = path?.FindIndex(p => p.Equals("raps", StringComparison.OrdinalIgnoreCase));
            string instance = "VIPER";
            if(rapsIdx != null && rapsIdx > -1 && path?.Count > rapsIdx + 1)
            {
                instance = path[(int)rapsIdx + 1];
            }
            string page = "";
            if (rapsIdx != null && rapsIdx > -1 && path?.Count > rapsIdx + 2)
            {
                page = path[(int)rapsIdx + 2];
            }
            ViewData["ViperLeftNav"] = await Nav(roleIdValid ? roleId : null,
                permIdValid ? permissionId : null,
                memberId,
                instance,
                page);
        }

        /// <summary>
        /// RAPS home page
        /// </summary>
        [Route("/[area]/{instance?}")]
        public async Task<ActionResult> Index(string? instance)
        {
            ViewData["KeyColumnName"] = "RoleId";
            instance ??= _securityService.GetDefaultInstanceForUser();

            return instance.ToUpper() switch
            {
                "VIPER" => await Task.Run(() => Redirect(string.Format("~/raps/VIPER/rolelist"))),
                "VMACS.VMTH" => await Task.Run(() => Redirect(string.Format("~/raps/VMACS.VMTH/rolelist"))),
                "VMACS.VMLF" => await Task.Run(() => Redirect(string.Format("~/raps/VMACS.VMLF/rolelist"))),
                "VMACS.UCVMCSD" => await Task.Run(() => Redirect(string.Format("~/raps/VMACS.UCVMCSD/rolelist"))),
                "VIPERFORMS" => await Task.Run(() => Redirect(string.Format("~/raps/ViperForms/rolelist"))),
                _ => await Task.Run(() => View("~/Views/Home/403.cshtml")),
            };
        }

        public async Task<NavMenu> Nav(int? roleId, int? permissionId, string? memberId, string instance = "VIPER", string page = "")
        {
            TblRole? selectedRole = (roleId != null) ? await _RAPSContext.TblRoles.FindAsync(roleId) : null;
            TblPermission? selectedPermission = (permissionId != null) ? await _RAPSContext.TblPermissions.FindAsync(permissionId) : null;
            VwAaudUser? selecteduser = (memberId != null) ? _RAPSContext.VwAaudUser.Single(r => r.MothraId == memberId) : null;

            var nav = new List<NavMenuItem>
            {
                new NavMenuItem() { MenuItemText = "Instances", IsHeader = true }
            };
            //Links to instances
            foreach (string inst in (new[] { "Viper", "ViperForms", "VMACS.VMTH", "VMACS.VMLF", "VMACS.UCVMCSD" }))
            {
                if(_securityService.IsAllowedTo("AccessInstance", inst))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = inst, MenuItemURL = "~/raps/" + inst + "/" + page });
                }
            }
            nav.Add(new NavMenuItem() { MenuItemText = "Roles", IsHeader = true });
            nav.Add(new NavMenuItem() { MenuItemText = "Role List", MenuItemURL = "Rolelist" });
            if (_securityService.IsAllowedTo("EditRoleMembership", instance))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Role Comparison", MenuItemURL = "RolePermissionsComparison" });
            }
            if (_securityService.IsAllowedTo("ViewRoles", instance))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Role Templates", MenuItemURL = "RoleTemplateList" });
            }
            if (selectedRole != null && _securityService.RoleBelongsToInstance(instance, selectedRole))
            {
                if ((selectedRole.Application == 0 && _securityService.IsAllowedTo("EditPermissions"))
                    || _securityService.IsAllowedTo("EditRoleMembership"))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = string.Format("Selected Role {0}", selectedRole.Role), IsHeader = true });
                }   
                if (selectedRole.Application == 0 && _securityService.IsAllowedTo("EditPermissions"))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = "Edit Permissions", MenuItemURL = "RolePermissions?roleId=" + selectedRole.RoleId });
                }
                if(_securityService.IsAllowedTo("EditRoleMembership"))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = "Role Members", MenuItemURL = "RoleMembers?roleId=" + selectedRole.RoleId });
                }
                
            }
            if(_securityService.IsAllowedTo("ManageAllPermissions") || _securityService.IsAllowedTo("ViewPermissions"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Permissions", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Permission List", MenuItemURL = "permissionlist" });
                if(selectedPermission != null)
                {
                    if (_securityService.IsAllowedTo("EditRolePermissions")
                        || _securityService.IsAllowedTo("EditMemberPermissions"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = string.Format("Selected Permission {0}", selectedPermission.Permission), IsHeader = true });
                    }
                    if(_securityService.IsAllowedTo("EditRolePermissions"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "Roles w/ Permission", MenuItemURL = "permissionRoles?permissionId=" + selectedPermission.PermissionId });
                    }
                    if (_securityService.IsAllowedTo("EditMemberPermissions"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "Users w/ Permission", MenuItemURL = "permissionMembers?permissionId=" + selectedPermission.PermissionId });
                    }
                }
            }
            if(_securityService.IsAllowedTo("UserLookup"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Users", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "User Lookup", MenuItemURL = "userSearch" });
                if(_securityService.IsAllowedTo("Clone", instance))
                {
                    nav.Add(new NavMenuItem() { MenuItemText = "Clone User Permissions", MenuItemURL = "UserClone" });
                }
                if(selecteduser != null)
                {
                    if (_securityService.IsAllowedTo("EditRoleMembership")
                        || _securityService.IsAllowedTo("EditMemberPermissions")
                        || _securityService.IsAllowedTo("RSOP"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = string.Format("Selected User {0}", selecteduser.DisplayFullName), IsHeader = true });
                    }
                    if(_securityService.IsAllowedTo("EditRoleMembership"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "User Roles", MenuItemURL = "memberRoles?memberId=" + selecteduser.MothraId });
                    }
                    if (_securityService.IsAllowedTo("EditMemberPermissions"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "User Permissions", MenuItemURL = "memberPermissions?memberId=" + selecteduser.MothraId });
                    }
                    if (_securityService.IsAllowedTo("RSOP"))
                    {
                        nav.Add(new NavMenuItem() { MenuItemText = "Combined Permissions", MenuItemURL = "RSOP?memberId=" + selecteduser.MothraId });
                    }
                }
            }
            if(_securityService.IsAllowedTo("ViewAuditTrail"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Admin", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "View Audit Trail", MenuItemURL = "AuditTrail" });
            }
            if(_securityService.IsAllowedTo("OUGroupsView"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Ad Group Management", IsHeader = true });
                nav.Add(new NavMenuItem() { MenuItemText = "Group List", MenuItemURL = "GroupList" });
            }
            if(_securityService.IsAllowedTo("Admin"))
            {
                nav.Add(new NavMenuItem() { MenuItemText = "Export to VMACS", MenuItemURL = "ExportToVMACS" });
                nav.Add(new NavMenuItem() { MenuItemText = "Update Role Views", MenuItemURL = "RoleViewUpdate" });
            }
            return new NavMenu("RAPS", nav);
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
        /// Show the list of the role templates
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        [Route("/[area]/{instance}/[action]")]
        [Permission(Allow = "RAPS.Admin,RAPS.ViewRoles")]
        public async Task<IActionResult> RoleTemplateList(string instance)
        {
            if(!_securityService.IsAllowedTo("ViewRoles", instance))
            {
                return await Task.Run(() => View("~/Views/Home/403.cshtml"));
            }
            ViewData["canEditRoleTemplates"] = _securityService.IsAllowedTo("EditRoleTemplates", instance);
            ViewData["canApplyTemplates"] = _securityService.IsAllowedTo("EditRoleMembership", instance);
            return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/Templates.cshtml"));
        }

        /// <summary>
        /// Apply a role template to one or more users
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        [Route("/[area]/{instance}/[action]")]
        [Permission(Allow = "RAPS.Admin,RAPS.EditRoleMembership")]
        public async Task<IActionResult> RoleTemplateApply(string instance)
        {
            if (!_securityService.IsAllowedTo("EditRoleMembership", instance))
            {
                return await Task.Run(() => View("~/Views/Home/403.cshtml"));
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/ApplyTemplate.cshtml"));
        }

        /// <summary>
        /// Link a role template to one or more roles
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        [Route("/[area]/{instance}/[action]")]
        [Permission(Allow = "RAPS.Admin,RAPS.EditRoles")]
        public async Task<IActionResult> RoleTemplateRoles(string instance)
        {
            if (!_securityService.IsAllowedTo("EditRoleTemplates", instance))
            {
                return await Task.Run(() => View("~/Views/Home/403.cshtml"));
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/TemplateRoles.cshtml"));
        }

        /// <summary>
        /// Show and manage the RAPS roles that delegate access to manage the membership of other roles
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin")]
        [Route("/[area]/{instance}/DelegateRoles")]
        public async Task<IActionResult> DelegateRoles()

        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/DelegateRoles.cshtml"));
        }

        /// <summary>
        /// Role members. Open to admins, IT people for VMACS roles, and Role "owners" for their roles.
        /// </summary>
        /// <param name="Instance">RAPS Instance</param>
        /// <param name="RoleId"></param>
        /// <returns></returns>
        [Route("/[area]/{instance}/[action]")]
        public async Task<IActionResult> RoleMembers(string instance, int RoleId)
        {
            ViewData["RoleId"] = RoleId;
            ViewData["canEditPermissions"] = _securityService.IsAllowedTo("EditMemberPermissions", instance);

            TblRole? Role = await _RAPSContext.TblRoles.FindAsync(RoleId);

            if (Role == null)
            {
                return NotFound();
            }
            if (_securityService.IsAllowedTo("EditRoleMembership", instance, Role))
            { 
                return View("~/Areas/RAPS/Views/Roles/Members.cshtml");
            }
            else
            {
                //TODO: Should probably have a deny access helper function that writes logs and sets view
                return View("~/Views/Home/403.cshtml");
            }
        }

        /// <summary>
        /// List permissions
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> PermissionList()
        {
            return await Task.Run(() =>
                UserHelper.HasPermission(_RAPSContext, UserHelper.GetCurrentUser(), "RAPS.Admin")
                    ? View("~/Areas/RAPS/Views/Permissions/ListAdmin.cshtml")
                    : View("~/Areas/RAPS/Views/Permissions/List.cshtml"));
        }

        /// <summary>
        /// List permissions for a role
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.ManageAllPermissions")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> RolePermissions(int roleId)
        {
            ViewData["roleId"] = roleId;
            return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/Permissions.cshtml"));
        }

        /// <summary>
        /// Compare permissions for two roles
        /// </summary>
        /// <returns></returns>
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> RolePermissionsComparison(string instance)
        {
            if (_securityService.IsAllowedTo("EditRoleMembership", instance))
            {
                return await Task.Run(() => View("~/Areas/RAPS/Views/Roles/PermissionComparison.cshtml"));
            }
            else
            {
                return await Task.Run(() => View("~/Views/Home/403.cshtml"));
            }
        }

        /// <summary>
        /// List members of a permission 
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.EditMemberPermissions")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> PermissionMembers(int? permissionId)
        {
            ViewData["permissionId"] = permissionId;

            TblPermission? permission = await _RAPSContext.TblPermissions.FindAsync(permissionId);

            if (permission == null)
            {
                return NotFound();
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Permissions/Members.cshtml"));
        }

        /// <summary>
        /// List roles for a permission 
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.EditRolePermissions")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> PermissionRoles(int? permissionId)
        {
            ViewData["permissionId"] = permissionId;

            TblPermission? permission = await _RAPSContext.TblPermissions.FindAsync(permissionId);

            if (permission == null)
            {
                return NotFound();
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Permissions/Roles.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> PermissionRolesRO(int? permissionId)
        {
            ViewData["permissionId"] = permissionId;

            TblPermission? permission = await _RAPSContext.TblPermissions.FindAsync(permissionId);

            if (permission == null)
            {
                return NotFound();
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Permissions/RolesRO.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin,RAPS.ViewPermissions")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> AllMembersWithPermission(int? permissionId)
        {
            ViewData["permissionId"] = permissionId;

            TblPermission? permission = await _RAPSContext.TblPermissions.FindAsync(permissionId);

            if (permission == null)
            {
                return NotFound();
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Permissions/AllMembers.cshtml"));
        }

        /**
         * User pages 
         */

        /// <summary>
        /// Search for users
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.UserLookup")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> UserSearch(string instance)
        {
            ViewData["canRSOP"] = _securityService.IsAllowedTo("RSOP", instance);
            ViewData["canEditRoleMembership"] = _securityService.IsAllowedTo("EditRoleMembership", instance);
            ViewData["canEditMemberPermissions"] = _securityService.IsAllowedTo("EditMemberPermissions", instance);
            ViewData["canViewHistory"] = _securityService.IsAllowedTo("ViewHistory", instance);
            return await Task.Run(() => View("~/Areas/RAPS/Views/Members/List.cshtml"));
        }

        /// <summary>
        /// Add/Update role assignments for a user
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.EditRoleMembership")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> MemberRoles(string instance)
        {
            ViewData["canEditPermissions"] = _securityService.IsAllowedTo("ManageAllPermissions", instance);
            //EditRoleMembership grants access only to the VMACS instance
            if (!_securityService.IsAllowedTo("EditRoleMembership", instance))
            {
                //TODO: Should probably have a deny access helper function that writes logs and sets view
                return View("~/Views/Home/403.cshtml");
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Members/Roles.cshtml"));
        }

        /// <summary>
        /// Add/Update permission assignments for a user
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.EditMemberPermissions")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> MemberPermissions()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Members/Permissions.cshtml"));
        }

        /// <summary>
        /// Show all permissions for a user, either from roles or permission assignments
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.RSOP")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> RSOP(string instance)
        {
            //RSOP grants access only to the VMACS instance
            if (!_securityService.IsAllowedTo("RSOP", instance))
            {
                //TODO: Should probably have a deny access helper function that writes logs and sets view
                return View("~/Views/Home/403.cshtml");
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Members/RSOP.cshtml"));
        }

        /// <summary>
        /// View history of changes to a user's role and permission assignments
        /// </summary>
        /// <returns></returns>
        [Permission(Allow = "RAPS.Admin,RAPS.EditRoleMembership")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> MemberHistory(string instance)
        {
            //EditRoleMembership grants access only to the VMACS instance
            if (!_securityService.IsAllowedTo("ViewHistory", instance))
            {
                //TODO: Should probably have a deny access helper function that writes logs and sets view
                return View("~/Views/Home/403.cshtml");
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Members/History.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin,RAPS.Clone")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> UserClone(string instance)
        {
            if (!_securityService.IsAllowedTo("Clone", instance))
            {
                return View("~/Views/Home/403.cshtml");
            }
            return await Task.Run(() => View("~/Areas/RAPS/Views/Members/Clone.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> ExportToVMACS(string? server = null , string? loginId = null, bool? debugOnly = false)
        {
            var vmacsExport = new VMACSExport(_RAPSContext);
            var servers = vmacsExport.GetServers();
            if (server != null && servers.Contains(server))
            {
                string inst = server.Split('-')[0];
                string serv = server.Split('-')[1];
                ViewData["Messages"] = await vmacsExport.ExportToVMACS(instance: inst, server: serv, debugOnly: debugOnly ?? true, loginId: loginId);
            }
            else
            {
                ViewData["Servers"] = servers;
            }
            
            return await Task.Run(() => View("~/Areas/RAPS/Views/Export.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> RoleViewUpdate()
        {
            ViewData["Messages"] = await new RoleViews(_RAPSContext)
                    .UpdateRoles(debugOnly: true);
            return await Task.Run(() => View("~/Areas/RAPS/Views/RoleViewUpdate.cshtml"));
        }
        
        [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> GroupList()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Groups/List.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> GroupRoles()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Groups/Roles.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> GroupMembers()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Groups/Members.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
        [Route("/[area]/{Instance}/[action]")]
        [SupportedOSPlatform("windows")]
        public async Task<IActionResult> GroupSync(int groupId)
        {
            OuGroup? group = _RAPSContext.OuGroups.Find(groupId);
            if (group != null)
            {
                _ = new OuGroupService(_RAPSContext).Sync(groupId, group.Name);
            }

            ViewData["Group"] = group;
            return await Task.Run(() => View("~/Areas/RAPS/Views/Groups/Sync.cshtml"));
        }

        [Permission(Allow = "RAPS.Admin,RAPS.OUGroupsView")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> CreateADGroup()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/Groups/CreateADGroup.cshtml"));
        }

        [Permission(Allow = "RAPS.ViewAuditTrail")]
        [Route("/[area]/{Instance}/[action]")]
        public async Task<IActionResult> AuditTrail()
        {
            return await Task.Run(() => View("~/Areas/RAPS/Views/AuditLog.cshtml"));
        }
    }
}
