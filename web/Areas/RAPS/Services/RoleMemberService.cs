using Viper.Areas.RAPS.Models;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

namespace Viper.Areas.RAPS.Services
{
    /// <summary>
    /// Class to handle common functions adding/removing/updating users to roles
    /// </summary>
    public class RoleMemberService
    {
        private readonly RAPSContext _context;
        public IUserHelper UserHelper;
        public RoleMemberService(RAPSContext context) 
        { 
            _context = context;
            UserHelper = new UserHelper();
        }

        public async Task<string?> AddMemberToRole(int roleId, string memberId, DateOnly? startDate, DateOnly? endDate, string? comment)
        {
            var tblRoleMemberExists = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (tblRoleMemberExists != null)
            {
                return "User is already a member of this role";
            }

            using var transaction = _context.Database.BeginTransaction();
            TblRoleMember tblRoleMember = new()
            {
                RoleId = roleId,
                MemberId = memberId,
                StartDate = startDate?.ToDateTime(new TimeOnly(0, 0, 0)),
                EndDate = endDate?.ToDateTime(new TimeOnly(0, 0, 0)),
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId
            };

            _context.TblRoleMembers.Add(tblRoleMember);
            _context.SaveChanges();
            new RAPSAuditService(_context).AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Create, comment);
            _context.SaveChanges();
            transaction.Commit();

            return null;
        }
    }
}
