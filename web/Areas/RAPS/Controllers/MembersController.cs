using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Viper.Areas.RAPS.Models;
using Viper.Areas.RAPS.Services;
using Viper.Classes.SQLContext;
using Viper.Models.RAPS;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Viper.Areas.RAPS.Controllers
{
    [Route("raps/{Instance=VIPER}/[controller]")]
    [ApiController]
    public class MembersController : ControllerBase
    {
        private readonly RAPSContext _context;
        private RAPSSecurityService _securityService;

        public MembersController(RAPSContext context)
        {
            _context = context;
            _securityService = new RAPSSecurityService(_context);
        }
        // GET: <Members>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberSearchResult>>> Get(string search, bool includeRoles=false, bool includePermissions=false)
        {
            var members = await _context.VwAaudUser
                    .Include(u => u.TblRoleMembers)
                    .Include(u => u.TblMemberPermissions)
                    .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                        || (u.MailId != null && u.MailId.Contains(search))
                        || (u.LoginId != null && u.LoginId.Contains(search))
                    )
                    .Where(u => u.Current)
                    .OrderBy(u => u.DisplayLastName)
                    .ThenBy(u => u.DisplayFirstName)
                    .ToListAsync();
            List<MemberSearchResult> results = new List<MemberSearchResult>();
            members.ForEach(m =>
            {
                results.Add(new MemberSearchResult()
                {
                    MemberId = m.MothraId,
                    DisplayFirstName = m.DisplayFirstName,
                    DisplayLastName = m.DisplayLastName,
                    LoginId = m.LoginId,
                    MailId = m.MailId,
                    CountPermissions = m.TblMemberPermissions.Count,
                    CountRoles = m.TblRoleMembers.Count,
                    Current = m.Current
                });
            });
            return results;
        }

        // GET <Members>/12345678
        [HttpGet("{memberId}")]
        public async Task<ActionResult<MemberSearchResult>> Get(string memberId)
        {
            var member = await _context.VwAaudUser.FirstOrDefaultAsync(u => u.MothraId == memberId);
            if(member == null)
            {
                return NotFound();
            }
            return new MemberSearchResult()
            {
                MemberId = member.MothraId,
                DisplayFirstName = member.DisplayFirstName,
                DisplayLastName = member.DisplayLastName,
                LoginId = member.LoginId,
                MailId = member.MailId,
                CountPermissions = member.TblMemberPermissions.Count,
                CountRoles = member.TblRoleMembers.Count,
                Current = member.Current
            };
        }
    }
}
