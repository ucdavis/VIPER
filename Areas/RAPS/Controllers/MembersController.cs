using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
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
        public IEnumerable<VwAaudUser> Get(string search)
        {
            return _context.VwAaudUser
                    .Where(u => (u.DisplayFirstName + " " + u.DisplayLastName).Contains(search)
                        || u.MailId.Contains(search)
                        || u.LoginId.Contains(search)
                    )
                    .Where(u => u.Current)
                    .OrderBy(u => u.DisplayLastName)
                    .ThenBy(u => u.DisplayFirstName)
                    .ToList();
        }

        // GET <Members>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }
    }
}
