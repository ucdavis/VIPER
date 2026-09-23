using Microsoft.EntityFrameworkCore;
using Viper.Areas.RAPS.Services;
using Viper.Areas.Students.Constants;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.Students.Services;

/// <summary>
/// Opens and closes a student self-service app, such as emergency contacts or career selection.
/// An app is open while the DVM student role holds its student permission, so each method takes
/// that permission's name.
/// </summary>
public class StudentAppAccessService : IStudentAppAccessService
{
    private readonly RAPSContext _rapsContext;
    private readonly IUserHelper _userHelper;
    private readonly RAPSAuditService _rapsAuditService;

    public StudentAppAccessService(RAPSContext rapsContext, IUserHelper userHelper)
    {
        _rapsContext = rapsContext;
        _userHelper = userHelper;
        _rapsAuditService = new RAPSAuditService(rapsContext, userHelper);
    }

    public async Task<bool> IsAppOpenAsync(string studentPermission)
    {
        var permissionId = await GetPermissionIdAsync(studentPermission);
        var roleId = await GetDvmStudentRoleIdAsync();
        return await _rapsContext.TblRolePermissions
            .AnyAsync(rp => rp.RoleId == roleId
                && rp.PermissionId == permissionId
                && rp.Access == 1);
    }

    /// <summary>
    /// Opens the app if it is closed and closes it if it is open. Returns whether it is now open.
    /// </summary>
    public async Task<bool> ToggleAppAccessAsync(string studentPermission)
    {
        var permissionId = await GetPermissionIdAsync(studentPermission);
        var roleId = await GetDvmStudentRoleIdAsync();
        var currentLoginId = _userHelper.GetCurrentUser()?.LoginId;

        var rolePermission = await _rapsContext.TblRolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId
                && rp.PermissionId == permissionId);

        // Closing the app REMOVES the role-permission row rather than setting
        // Access = 0. In RAPS, a role Deny (Access = 0) overrides an individual
        // member Allow, which would silently break individual grants.
        var isCurrentlyOpen = rolePermission != null && rolePermission.Access == 1;

        if (isCurrentlyOpen)
        {
            _rapsAuditService.AuditRolePermissionChange(rolePermission!, RAPSAuditService.AuditActionType.Delete);
            _rapsContext.TblRolePermissions.Remove(rolePermission!);
            await _rapsContext.SaveChangesAsync();
            return false;
        }

        if (rolePermission == null)
        {
            rolePermission = new TblRolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                Access = 1,
                ModTime = DateTime.Now,
                ModBy = currentLoginId
            };
            _rapsContext.TblRolePermissions.Add(rolePermission);
            _rapsAuditService.AuditRolePermissionChange(rolePermission, RAPSAuditService.AuditActionType.Create);
        }
        else
        {
            // Legacy row with Access = 0 from previous toggle behavior — flip to 1.
            rolePermission.Access = 1;
            rolePermission.ModTime = DateTime.Now;
            rolePermission.ModBy = currentLoginId;
            _rapsAuditService.AuditRolePermissionChange(rolePermission, RAPSAuditService.AuditActionType.Update);
        }
        await _rapsContext.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetPermissionIdAsync(string permissionName)
    {
        var permission = await _rapsContext.TblPermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Permission == permissionName);
        return permission?.PermissionId
            ?? throw new InvalidOperationException(
                $"RAPS permission '{permissionName}' not found");
    }

    private async Task<int> GetDvmStudentRoleIdAsync()
    {
        var role = await _rapsContext.TblRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Role == StudentRoles.DvmStudents);
        return role?.RoleId
            ?? throw new InvalidOperationException(
                $"RAPS role '{StudentRoles.DvmStudents}' not found");
    }
}
