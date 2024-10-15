using Microsoft.AspNetCore.Mvc;
using Viper.Areas.CTS.Services;
using Viper.Classes;
using Viper.Classes.SQLContext;
using Web.Authorization;

namespace Viper.Areas.CTS.Controllers
{
    [Route("/api/cts/encounters")]
    [Permission(Allow = "SVMSecure.CTS")]
    public class EncounterController : ApiController
    {
        private readonly VIPERContext context;
        private AuditService auditService;
        private CtsSecurityService ctsSecurityService;

        public EncounterController(VIPERContext _context, RAPSContext rapsContext)
        {
            context = _context;
            auditService = new AuditService(context);
            ctsSecurityService = new CtsSecurityService(rapsContext, _context);
        }

    }
}
