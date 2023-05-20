using Viper.Models.RAPS;
using static Viper.Areas.RAPS.Services.RAPSAuditService;

namespace Viper.Areas.RAPS.Services
{
    public class RAPSAuditServiceWrapper : IRAPSAuditServiceWrapper 
    {
        private readonly RAPSAuditService _RAPSAuditService;

        public RAPSAuditServiceWrapper(RAPSAuditService myDependency)
        {
            _RAPSAuditService = myDependency;
        }

        public void AuditRoleChange(TblRole role, AuditActionType actionType)
        {
            _RAPSAuditService.AuditRoleChange(role, actionType);

        }

        public void AuditPermissionChange(TblPermission permission, AuditActionType actionType)
        {
            _RAPSAuditService.AuditPermissionChange(permission, actionType);

        }

        public void AuditRoleMemberChange(TblRoleMember roleMember, AuditActionType actionType, string? comment)
        {
            _RAPSAuditService.AuditRoleMemberChange(roleMember, actionType, comment);

        }
    }

    public interface IRAPSAuditServiceWrapper
    {
        void AuditRoleChange(TblRole role, AuditActionType actionType);

        void AuditPermissionChange(TblPermission permission, AuditActionType actionType);

        void AuditRoleMemberChange(TblRoleMember roleMember, AuditActionType actionType, string? comment);
    }

}
