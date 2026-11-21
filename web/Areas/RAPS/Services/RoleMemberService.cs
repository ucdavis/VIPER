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
        public IUserHelper UserHelper { get; private set; }
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

            await InsertMember(roleId, memberId, startDate, endDate, comment);
            return null;
        }

        private async Task InsertMember(int roleId, string memberId, DateOnly? startDate, DateOnly? endDate, string? comment)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            TblRoleMember tblRoleMember = new()
            {
                RoleId = roleId,
                MemberId = memberId,
                AddDate = DateTime.Now,
                StartDate = startDate?.ToDateTime(new TimeOnly(0, 0, 0)),
                EndDate = endDate?.ToDateTime(new TimeOnly(0, 0, 0)),
                ModTime = DateTime.Now,
                ModBy = UserHelper.GetCurrentUser()?.LoginId
            };

            _context.TblRoleMembers.Add(tblRoleMember);
            await _context.SaveChangesAsync();
            new RAPSAuditService(_context).AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Create, comment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        /// <summary>
        /// Give the user the role from today with no end date. A row outside its dates is
        /// reactivated, since AddMemberToRole refuses any existing row.
        /// </summary>
        public async Task AddOrRenewMember(int roleId, string memberId, string? comment)
        {
            var tblRoleMember = await _context.TblRoleMembers.FindAsync(roleId, memberId);
            if (tblRoleMember == null)
            {
                await InsertMember(roleId, memberId, null, null, comment);
                return;
            }

            tblRoleMember.StartDate = null;
            tblRoleMember.EndDate = null;
            tblRoleMember.ModTime = DateTime.Now;
            tblRoleMember.ModBy = UserHelper.GetCurrentUser()?.LoginId;
            new RAPSAuditService(_context).AuditRoleMemberChange(tblRoleMember, RAPSAuditService.AuditActionType.Update, comment);
            await _context.SaveChangesAsync();
        }
    }
}
